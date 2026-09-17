using System.Diagnostics;
using System.Text;
using Delivery.Infrastructure.Observability;
using Delivery.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuickOrder.Contracts.Messaging;
using RabbitMQ.Client;

namespace Delivery.Infrastructure.Outbox;

/// <summary>
/// Polls the Outbox table and publishes pending rows to RabbitMQ. See
/// Catalog/Ordering's OutboxPublisher for the full rationale — same two-level retry
/// (per message with backoff, and the whole loop reconnecting on any failure so a
/// BackgroundService exception never takes the host down).
/// </summary>
public sealed class OutboxPublisher(
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqOptions> options,
    ILogger<OutboxPublisher> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private const int BatchSize = 20;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var attempt = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunPublishLoopAsync(stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                attempt++;
                var delay = TimeSpan.FromSeconds(Math.Min(Math.Pow(2, attempt), 30));
                logger.LogWarning(exception, "Outbox publisher loop failed (attempt {Attempt}), retrying in {Delay}", attempt, delay);
                await Task.Delay(delay, stoppingToken);
            }
        }
    }

    private async Task RunPublishLoopAsync(CancellationToken stoppingToken)
    {
        var rabbitMqOptions = options.Value;

        var factory = new ConnectionFactory
        {
            HostName = rabbitMqOptions.HostName,
            Port = rabbitMqOptions.Port,
            UserName = rabbitMqOptions.UserName,
            Password = rabbitMqOptions.Password,
        };

        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await channel.ExchangeDeclareAsync(
            rabbitMqOptions.Exchange, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var publishedAny = await PublishPendingBatchAsync(channel, rabbitMqOptions.Exchange, stoppingToken);

            if (!publishedAny)
                await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task<bool> PublishPendingBatchAsync(IChannel channel, string exchange, CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>();

        var now = DateTimeOffset.UtcNow;
        var pending = await dbContext.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null && (m.NextAttemptAtUtc == null || m.NextAttemptAtUtc <= now))
            .OrderBy(m => m.OccurredOnUtc)
            .Take(BatchSize)
            .ToListAsync(stoppingToken);

        if (pending.Count == 0)
            return false;

        foreach (var message in pending)
        {
            try
            {
                await PublishOneAsync(channel, exchange, message, stoppingToken);
                message.MarkProcessed(DateTimeOffset.UtcNow);
            }
            catch (Exception exception) when (message.CanRetry)
            {
                logger.LogWarning(exception, "Failed to publish outbox message {MessageId} (attempt {Attempt}/{MaxRetries})",
                    message.Id, message.RetryCount + 1, OutboxMessage.MaxRetries);
                message.MarkFailed(exception.Message, DateTimeOffset.UtcNow);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Outbox message {MessageId} exceeded {MaxRetries} retries, giving up",
                    message.Id, OutboxMessage.MaxRetries);
                message.MarkFailed(exception.Message, DateTimeOffset.UtcNow);
            }
        }

        await dbContext.SaveChangesAsync(stoppingToken);
        return true;
    }

    private static async Task PublishOneAsync(IChannel channel, string exchange, OutboxMessage message, CancellationToken cancellationToken)
    {
        var parentContext = message.TraceParent is not null && ActivityContext.TryParse(message.TraceParent, null, out var parsed)
            ? parsed
            : default;

        using var activity = DeliveryActivitySource.Instance.StartActivity(
            $"{exchange} publish {message.Type}", ActivityKind.Producer, parentContext);

        var headers = new Dictionary<string, object?>();
        TraceContextPropagation.Inject(activity, headers);

        var properties = new BasicProperties { Persistent = true, Headers = headers };
        var body = Encoding.UTF8.GetBytes(message.Content);

        await channel.BasicPublishAsync(exchange, message.Type, mandatory: false, properties, body, cancellationToken);
    }
}

using System.Diagnostics;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ordering.Infrastructure.Observability;
using Ordering.Infrastructure.Persistence;
using QuickOrder.Contracts.Messaging;
using RabbitMQ.Client;

namespace Ordering.Infrastructure.Outbox;

/// <summary>
/// Polls the Outbox table and publishes pending rows to RabbitMQ. Retries a broker
/// hiccup with exponential backoff per message (see OutboxMessage.MarkFailed); after
/// MaxRetries, a message is left behind with its Error populated for manual/alerting
/// follow-up instead of blocking the rest of the queue forever (a poison-message guard).
/// </summary>
public sealed class OutboxPublisher(
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqOptions> options,
    ILogger<OutboxPublisher> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private const int BatchSize = 20;

    /// <summary>
    /// Everything — connecting, declaring the exchange, the publish loop — runs inside
    /// one outer retry loop. A BackgroundService whose ExecuteAsync throws stops the
    /// whole host by default (ASP.NET Core's generic host), so a message broker being
    /// temporarily down must never escape as an unhandled exception here; it should
    /// just mean "try again shortly", the same as any other transient infra failure.
    /// </summary>
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
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();

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

        using var activity = OrderingActivitySource.Instance.StartActivity(
            $"{exchange} publish {message.Type}", ActivityKind.Producer, parentContext);

        var headers = new Dictionary<string, object?>();
        TraceContextPropagation.Inject(activity, headers);

        var properties = new BasicProperties { Persistent = true, Headers = headers };
        var body = Encoding.UTF8.GetBytes(message.Content);

        await channel.BasicPublishAsync(exchange, message.Type, mandatory: false, properties, body, cancellationToken);
    }
}

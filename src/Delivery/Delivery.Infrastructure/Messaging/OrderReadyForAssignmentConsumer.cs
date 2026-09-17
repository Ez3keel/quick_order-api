using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Delivery.Application.Assignments.Commands.AssignCourier;
using Delivery.Application.Assignments.Exceptions;
using Delivery.Infrastructure.Observability;
using Delivery.Infrastructure.Outbox;
using Delivery.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuickOrder.Contracts.Messaging;
using QuickOrder.Contracts.Ordering;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Delivery.Infrastructure.Messaging;

/// <summary>
/// The single consumer of "OrderReadyForAssignmentIntegrationEvent". Prefetch is 1 and
/// there is exactly one consumer instance per queue — that's what makes assignment
/// race-free without a distributed lock (see docs, item 7): the broker itself
/// serializes delivery, so AssignCourierCommandHandler never runs concurrently with
/// itself in this process.
///
/// Retry topology (fully explicit, not relying on RabbitMQ's x-death header):
/// main queue -&gt; on failure, republish with an incremented x-retry-count header to a
/// delay queue (TTL + dead-letter back to the main queue) -&gt; after MaxRetries,
/// publish straight to the DLQ instead of retrying again.
/// </summary>
public sealed class OrderReadyForAssignmentConsumer(
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqOptions> deliveryOptions,
    IOptions<OrderingExchangeOptions> orderingOptions,
    ILogger<OrderReadyForAssignmentConsumer> logger) : BackgroundService
{
    public const string MainQueue = "delivery.order-ready-for-assignment";
    public const string RetryQueue = "delivery.order-ready-for-assignment.retry";
    public const string DeadLetterQueue = "delivery.order-ready-for-assignment.dlq";
    private const int MaxRetries = 3;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var attempt = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunConsumeLoopAsync(stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                attempt++;
                var delay = TimeSpan.FromSeconds(Math.Min(Math.Pow(2, attempt), 30));
                logger.LogWarning(exception, "Consumer loop failed (attempt {Attempt}), retrying in {Delay}", attempt, delay);
                await Task.Delay(delay, stoppingToken);
            }
        }
    }

    private async Task RunConsumeLoopAsync(CancellationToken stoppingToken)
    {
        var options = deliveryOptions.Value;
        var orderingExchange = orderingOptions.Value.Exchange;

        var factory = new ConnectionFactory
        {
            HostName = options.HostName,
            Port = options.Port,
            UserName = options.UserName,
            Password = options.Password,
        };

        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await channel.BasicQosAsync(0, prefetchCount: 1, global: false, stoppingToken);

        await DeclareTopologyAsync(channel, orderingExchange, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, delivery) => await HandleDeliveryAsync(channel, delivery, stoppingToken);

        await channel.BasicConsumeAsync(MainQueue, autoAck: false, consumer, stoppingToken);

        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }

    private static async Task DeclareTopologyAsync(IChannel channel, string orderingExchange, CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(orderingExchange, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(MainQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(
            MainQueue, orderingExchange, RoutingKey.For<OrderReadyForAssignmentIntegrationEvent>(), cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(RetryQueue, durable: true, exclusive: false, autoDelete: false, arguments: new Dictionary<string, object?>
        {
            ["x-message-ttl"] = (int)RetryDelay.TotalMilliseconds,
            ["x-dead-letter-exchange"] = "",
            ["x-dead-letter-routing-key"] = MainQueue,
        }, cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(DeadLetterQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
    }

    private async Task HandleDeliveryAsync(IChannel channel, BasicDeliverEventArgs delivery, CancellationToken stoppingToken)
    {
        var json = Encoding.UTF8.GetString(delivery.Body.ToArray());
        var integrationEvent = JsonSerializer.Deserialize<OrderReadyForAssignmentIntegrationEvent>(json);

        if (integrationEvent is null)
        {
            logger.LogError("Could not deserialize message on {Queue}, sending straight to DLQ", MainQueue);
            await PublishToQueueAsync(channel, DeadLetterQueue, delivery.Body, headers: null, stoppingToken);
            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, stoppingToken);
            return;
        }

        // Links this consumer's span to whatever produced the message (Ordering's
        // OutboxPublisher span) so the trace stays connected across the queue hop —
        // RabbitMQ carries no trace context on its own, the header was put there
        // manually by the producer (see TraceContextPropagation).
        var parentContext = TraceContextPropagation.Extract(delivery.BasicProperties.Headers);
        using var activity = DeliveryActivitySource.Instance.StartActivity(
            $"{MainQueue} process", ActivityKind.Consumer, parentContext);

        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>();

        if (await dbContext.ProcessedMessages.AnyAsync(m => m.EventId == integrationEvent.EventId, stoppingToken))
        {
            logger.LogInformation("Event {EventId} already processed, acking without reprocessing", integrationEvent.EventId);
            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, stoppingToken);
            return;
        }

        try
        {
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            var courierId = await sender.Send(
                new AssignCourierCommand(integrationEvent.OrderId, integrationEvent.RestaurantId), stoppingToken);

            dbContext.ProcessedMessages.Add(ProcessedMessage.Create(
                integrationEvent.EventId, nameof(OrderReadyForAssignmentIntegrationEvent), DateTimeOffset.UtcNow));
            await dbContext.SaveChangesAsync(stoppingToken);

            logger.LogInformation("Assigned courier {CourierId} to order {OrderId}", courierId, integrationEvent.OrderId);
            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, stoppingToken);
        }
        catch (Exception exception)
        {
            var isTransient = exception is NoCourierAvailableException;
            if (!isTransient)
                logger.LogError(exception, "Failed to assign courier for order {OrderId}", integrationEvent.OrderId);
            else
                logger.LogInformation("No courier available for order {OrderId} yet, will retry", integrationEvent.OrderId);

            await RetryOrDeadLetterAsync(channel, delivery, stoppingToken);
        }
    }

    private async Task RetryOrDeadLetterAsync(IChannel channel, BasicDeliverEventArgs delivery, CancellationToken stoppingToken)
    {
        var retryCount = GetRetryCount(delivery.BasicProperties.Headers);

        if (retryCount < MaxRetries)
        {
            var headers = new Dictionary<string, object?> { ["x-retry-count"] = retryCount + 1 };
            await PublishToQueueAsync(channel, RetryQueue, delivery.Body, headers, stoppingToken);
        }
        else
        {
            logger.LogError("Message exceeded {MaxRetries} retries, sending to {DeadLetterQueue}", MaxRetries, DeadLetterQueue);
            await PublishToQueueAsync(channel, DeadLetterQueue, delivery.Body, headers: null, stoppingToken);
        }

        await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, stoppingToken);
    }

    private static int GetRetryCount(IDictionary<string, object?>? headers)
    {
        if (headers is null || !headers.TryGetValue("x-retry-count", out var value) || value is null)
            return 0;

        return Convert.ToInt32(value);
    }

    private static async Task PublishToQueueAsync(
        IChannel channel, string queue, ReadOnlyMemory<byte> body, Dictionary<string, object?>? headers, CancellationToken cancellationToken)
    {
        var properties = new BasicProperties { Persistent = true };
        if (headers is not null)
            properties.Headers = headers;

        await channel.BasicPublishAsync(exchange: "", routingKey: queue, mandatory: false, properties, body, cancellationToken);
    }
}

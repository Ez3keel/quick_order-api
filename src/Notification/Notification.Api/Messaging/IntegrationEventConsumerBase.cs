using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notification.Api.Observability;
using QuickOrder.Contracts;
using QuickOrder.Contracts.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Notification.Api.Messaging;

/// <summary>
/// Shared shape for the two consumers in this service. Failures just nack-and-requeue
/// (no delay queue, no DLQ) — unlike Delivery's consumer (Fase 4), losing or delaying
/// a push notification is a UX blemish, not a correctness problem, so the extra retry
/// machinery isn't worth it here. See RedisEventDeduplicator for why dedup lives in
/// Redis instead of a ProcessedMessages table.
/// </summary>
public abstract class IntegrationEventConsumerBase<TEvent>(
    IOptions<RabbitMqOptions> options,
    IEventDeduplicator deduplicator,
    ILogger logger) : BackgroundService
    where TEvent : IIntegrationEvent
{
    protected abstract string Exchange { get; }
    protected abstract string QueueName { get; }
    protected abstract string BindingRoutingKey { get; }

    protected abstract Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken);

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
                logger.LogWarning(exception, "Consumer loop for {Queue} failed (attempt {Attempt}), retrying in {Delay}",
                    QueueName, attempt, delay);
                await Task.Delay(delay, stoppingToken);
            }
        }
    }

    private async Task RunConsumeLoopAsync(CancellationToken stoppingToken)
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
        await channel.BasicQosAsync(0, prefetchCount: 10, global: false, stoppingToken);

        await channel.ExchangeDeclareAsync(Exchange, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
        await channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await channel.QueueBindAsync(QueueName, Exchange, BindingRoutingKey, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, delivery) => await HandleDeliveryAsync(channel, delivery, stoppingToken);

        await channel.BasicConsumeAsync(QueueName, autoAck: false, consumer, stoppingToken);

        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }

    private async Task HandleDeliveryAsync(IChannel channel, BasicDeliverEventArgs delivery, CancellationToken stoppingToken)
    {
        try
        {
            var json = Encoding.UTF8.GetString(delivery.Body.ToArray());
            var integrationEvent = JsonSerializer.Deserialize<TEvent>(json);

            if (integrationEvent is null)
            {
                logger.LogError("Could not deserialize message on {Queue}, discarding", QueueName);
                await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, stoppingToken);
                return;
            }

            if (!await deduplicator.TryMarkProcessedAsync(integrationEvent.EventId, stoppingToken))
            {
                logger.LogInformation("Event {EventId} already processed, acking without re-pushing", integrationEvent.EventId);
                await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, stoppingToken);
                return;
            }

            var parentContext = TraceContextPropagation.Extract(delivery.BasicProperties.Headers);
            using var activity = NotificationActivitySource.Instance.StartActivity(
                $"{QueueName} process", ActivityKind.Consumer, parentContext);

            await HandleAsync(integrationEvent, stoppingToken);
            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, stoppingToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to push notification for a message on {Queue}, requeueing", QueueName);
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: true, stoppingToken);
        }
    }
}

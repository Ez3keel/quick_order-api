using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Notification.Api.Hubs;
using QuickOrder.Contracts.Messaging;
using QuickOrder.Contracts.Ordering;

namespace Notification.Api.Messaging;

/// <summary>Pushes every order status transition to whoever is subscribed to that
/// order's group — the customer's app, mainly.</summary>
public sealed class OrderStatusChangedConsumer(
    IOptions<RabbitMqOptions> options,
    IEventDeduplicator deduplicator,
    IHubContext<OrderTrackingHub> hubContext,
    ILogger<OrderStatusChangedConsumer> logger)
    : IntegrationEventConsumerBase<OrderStatusChangedIntegrationEvent>(options, deduplicator, logger)
{
    protected override string Exchange => options.Value.OrderingExchange;
    protected override string QueueName => "notification.order-status-changed";
    protected override string BindingRoutingKey => RoutingKey.For<OrderStatusChangedIntegrationEvent>();

    protected override Task HandleAsync(OrderStatusChangedIntegrationEvent integrationEvent, CancellationToken cancellationToken) =>
        hubContext.Clients.Group(GroupNames.ForOrder(integrationEvent.OrderId)).SendAsync(
            "OrderStatusChanged",
            new { integrationEvent.OrderId, integrationEvent.PreviousStatus, integrationEvent.NewStatus },
            cancellationToken);
}

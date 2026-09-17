using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Notification.Api.Hubs;
using QuickOrder.Contracts.Delivery;
using QuickOrder.Contracts.Messaging;

namespace Notification.Api.Messaging;

/// <summary>Pushes to two groups: the order's (so the customer sees "a courier is on
/// the way") and the courier's own (so their app knows to start tracking this
/// delivery — that app joined `courier-{courierId}` on login, before any order
/// existed, since it has no order id to subscribe to yet).</summary>
public sealed class CourierAssignedConsumer(
    IOptions<RabbitMqOptions> options,
    IEventDeduplicator deduplicator,
    IHubContext<OrderTrackingHub> hubContext,
    ILogger<CourierAssignedConsumer> logger)
    : IntegrationEventConsumerBase<CourierAssignedIntegrationEvent>(options, deduplicator, logger)
{
    protected override string Exchange => options.Value.DeliveryExchange;
    protected override string QueueName => "notification.courier-assigned";
    protected override string BindingRoutingKey => RoutingKey.For<CourierAssignedIntegrationEvent>();

    protected override async Task HandleAsync(CourierAssignedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        await hubContext.Clients.Group(GroupNames.ForOrder(integrationEvent.OrderId)).SendAsync(
            "CourierAssigned",
            new { integrationEvent.OrderId, integrationEvent.CourierId },
            cancellationToken);

        await hubContext.Clients.Group(GroupNames.ForCourier(integrationEvent.CourierId)).SendAsync(
            "OrderAssignedToYou",
            new { integrationEvent.OrderId, integrationEvent.AssignmentId },
            cancellationToken);
    }
}

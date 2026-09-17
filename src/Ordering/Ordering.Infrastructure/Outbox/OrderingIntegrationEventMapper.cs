using System.Diagnostics;
using System.Text.Json;
using Ordering.Domain.Common;
using Ordering.Domain.Orders.Events;
using QuickOrder.Contracts.Messaging;
using QuickOrder.Contracts.Ordering;

namespace Ordering.Infrastructure.Outbox;

/// <summary>Translates a domain event (internal to Ordering.Domain) into the
/// integration event contract published on the wire.</summary>
public static class OrderingIntegrationEventMapper
{
    public static OutboxMessage? TryMapToOutboxMessage(IDomainEvent domainEvent)
    {
        object? integrationEvent = domainEvent switch
        {
            OrderPlacedEvent e => new OrderPlacedIntegrationEvent(
                Guid.NewGuid(), e.OccurredOn, e.OrderId.Value, e.CustomerId, e.RestaurantId),

            OrderReadyForAssignmentEvent e => new OrderReadyForAssignmentIntegrationEvent(
                Guid.NewGuid(), e.OccurredOn, e.OrderId.Value, e.RestaurantId),

            OrderStatusChangedEvent e => new OrderStatusChangedIntegrationEvent(
                Guid.NewGuid(), e.OccurredOn, e.OrderId.Value, e.PreviousStatus.ToString(), e.NewStatus.ToString()),

            _ => null,
        };

        if (integrationEvent is null)
            return null;

        var routingKey = RoutingKey.For(integrationEvent.GetType());
        var content = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType());

        return OutboxMessage.Create(Guid.NewGuid(), routingKey, content, domainEvent.OccurredOn, Activity.Current?.Id);
    }
}

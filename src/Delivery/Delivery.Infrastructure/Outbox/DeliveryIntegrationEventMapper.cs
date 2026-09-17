using System.Diagnostics;
using System.Text.Json;
using Delivery.Domain.Assignments.Events;
using Delivery.Domain.Common;
using QuickOrder.Contracts.Delivery;
using QuickOrder.Contracts.Messaging;

namespace Delivery.Infrastructure.Outbox;

public static class DeliveryIntegrationEventMapper
{
    public static OutboxMessage? TryMapToOutboxMessage(IDomainEvent domainEvent)
    {
        object? integrationEvent = domainEvent switch
        {
            CourierAssignedEvent e => new CourierAssignedIntegrationEvent(
                Guid.NewGuid(), e.OccurredOn, e.AssignmentId.Value, e.OrderId, e.CourierId.Value),

            DeliveryCompletedEvent e => new DeliveryCompletedIntegrationEvent(
                Guid.NewGuid(), e.OccurredOn, e.AssignmentId.Value, e.OrderId),

            _ => null,
        };

        if (integrationEvent is null)
            return null;

        var routingKey = RoutingKey.For(integrationEvent.GetType());
        var content = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType());

        return OutboxMessage.Create(Guid.NewGuid(), routingKey, content, domainEvent.OccurredOn, Activity.Current?.Id);
    }
}

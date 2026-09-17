namespace QuickOrder.Contracts.Delivery;

/// <summary>Consumed by Ordering (Fase 6+) to move the order to OutForDelivery, and
/// by Notification to push the update to the customer and courier.</summary>
public sealed record CourierAssignedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid AssignmentId,
    Guid OrderId,
    Guid CourierId) : IIntegrationEvent;

public sealed record DeliveryCompletedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid AssignmentId,
    Guid OrderId) : IIntegrationEvent;

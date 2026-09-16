namespace QuickOrder.Contracts.Ordering;

public sealed record OrderPlacedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid OrderId,
    Guid CustomerId,
    Guid RestaurantId) : IIntegrationEvent;

/// <summary>Consumed by the Delivery Assignment Worker's single-consumer queue (Fase 4)
/// to pick a courier for this order.</summary>
public sealed record OrderReadyForAssignmentIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid OrderId,
    Guid RestaurantId) : IIntegrationEvent;

public sealed record OrderStatusChangedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid OrderId,
    string PreviousStatus,
    string NewStatus) : IIntegrationEvent;

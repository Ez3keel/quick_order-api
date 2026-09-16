using Ordering.Domain.Common;

namespace Ordering.Domain.Orders.Events;

/// <summary>
/// Raised on every status transition. The Notification Service consumes this to push
/// real-time updates to both the customer and the assigned courier over SignalR.
/// </summary>
public sealed record OrderStatusChangedEvent(
    OrderId OrderId,
    OrderStatus PreviousStatus,
    OrderStatus NewStatus,
    DateTimeOffset OccurredOn) : IDomainEvent;

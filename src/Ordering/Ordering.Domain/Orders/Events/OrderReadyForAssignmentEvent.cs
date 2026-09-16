using Ordering.Domain.Common;

namespace Ordering.Domain.Orders.Events;

/// <summary>
/// Raised when food is ready and the order needs a courier. This is the event the
/// Delivery Assignment Worker consumes from its queue to run the assignment algorithm.
/// </summary>
public sealed record OrderReadyForAssignmentEvent(OrderId OrderId, Guid RestaurantId, DateTimeOffset OccurredOn)
    : IDomainEvent;

using Ordering.Domain.Common;

namespace Ordering.Domain.Orders.Events;

public sealed record OrderPlacedEvent(OrderId OrderId, Guid CustomerId, Guid RestaurantId, DateTimeOffset OccurredOn)
    : IDomainEvent;

using Delivery.Domain.Common;
using Delivery.Domain.Couriers;

namespace Delivery.Domain.Assignments.Events;

/// <summary>
/// Raised when the assignment worker successfully pairs an order with a courier.
/// Published as an integration event so the Ordering service can move the order
/// to OutForDelivery and the Notification service can push the update.
/// </summary>
public sealed record CourierAssignedEvent(
    DeliveryAssignmentId AssignmentId,
    Guid OrderId,
    CourierId CourierId,
    DateTimeOffset OccurredOn) : IDomainEvent;

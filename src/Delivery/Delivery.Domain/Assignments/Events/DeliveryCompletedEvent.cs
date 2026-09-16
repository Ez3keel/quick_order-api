using Delivery.Domain.Common;

namespace Delivery.Domain.Assignments.Events;

public sealed record DeliveryCompletedEvent(
    DeliveryAssignmentId AssignmentId,
    Guid OrderId,
    DateTimeOffset OccurredOn) : IDomainEvent;

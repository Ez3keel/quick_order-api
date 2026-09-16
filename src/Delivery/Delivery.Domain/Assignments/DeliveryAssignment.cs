using Delivery.Domain.Assignments.Events;
using Delivery.Domain.Assignments.Exceptions;
using Delivery.Domain.Common;
using Delivery.Domain.Couriers;

namespace Delivery.Domain.Assignments;

/// <summary>
/// Links an order to the courier that will deliver it. References both by id only:
/// Order belongs to the Ordering service, Courier is a separate aggregate in this
/// same service, so no other aggregate is loaded/mutated in the same transaction.
/// </summary>
public sealed class DeliveryAssignment : AggregateRoot<DeliveryAssignmentId>
{
    private static readonly Dictionary<DeliveryAssignmentStatus, DeliveryAssignmentStatus[]> AllowedTransitions = new()
    {
        [DeliveryAssignmentStatus.PendingPickup] = [DeliveryAssignmentStatus.InTransit],
        [DeliveryAssignmentStatus.InTransit] = [DeliveryAssignmentStatus.Completed],
        [DeliveryAssignmentStatus.Completed] = [],
    };

    public Guid OrderId { get; private init; }
    public CourierId CourierId { get; private init; }
    public DeliveryAssignmentStatus Status { get; private set; }
    public DateTimeOffset AssignedAt { get; private init; }
    public DateTimeOffset? CompletedAt { get; private set; }

    private DeliveryAssignment() { }

    private DeliveryAssignment(DeliveryAssignmentId id, Guid orderId, CourierId courierId, DateTimeOffset now)
        : base(id)
    {
        OrderId = orderId;
        CourierId = courierId;
        Status = DeliveryAssignmentStatus.PendingPickup;
        AssignedAt = now;
    }

    /// <summary>Creates the assignment. Callers must have already reserved the courier
    /// (<see cref="Courier.Reserve"/>) before calling this, since a courier is a separate
    /// aggregate and its own invariants (only one active assignment at a time) live there.</summary>
    public static DeliveryAssignment Create(Guid orderId, CourierId courierId, DateTimeOffset now)
    {
        if (orderId == Guid.Empty)
            throw new ArgumentException("OrderId is required.", nameof(orderId));

        var assignment = new DeliveryAssignment(DeliveryAssignmentId.New(), orderId, courierId, now);
        assignment.Raise(new CourierAssignedEvent(assignment.Id, orderId, courierId, now));
        return assignment;
    }

    public void ConfirmPickup(DateTimeOffset now) => TransitionTo(DeliveryAssignmentStatus.InTransit, now);

    public void Complete(DateTimeOffset now)
    {
        TransitionTo(DeliveryAssignmentStatus.Completed, now);
        CompletedAt = now;
        Raise(new DeliveryCompletedEvent(Id, OrderId, now));
    }

    private void TransitionTo(DeliveryAssignmentStatus next, DateTimeOffset now)
    {
        if (!AllowedTransitions[Status].Contains(next))
            throw new InvalidAssignmentStateTransitionException(Status, next);

        Status = next;
    }
}

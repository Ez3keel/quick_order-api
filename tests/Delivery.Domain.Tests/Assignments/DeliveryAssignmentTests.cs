using Delivery.Domain.Assignments;
using Delivery.Domain.Assignments.Events;
using Delivery.Domain.Assignments.Exceptions;
using Delivery.Domain.Couriers;

namespace Delivery.Domain.Tests.Assignments;

public class DeliveryAssignmentTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 16, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_WithValidData_StartsPendingPickupAndRaisesEvent()
    {
        var courierId = CourierId.New();
        var orderId = Guid.NewGuid();

        var assignment = DeliveryAssignment.Create(orderId, courierId, Now);

        Assert.Equal(DeliveryAssignmentStatus.PendingPickup, assignment.Status);
        var raised = Assert.Single(assignment.DomainEvents);
        var assignedEvent = Assert.IsType<CourierAssignedEvent>(raised);
        Assert.Equal(orderId, assignedEvent.OrderId);
        Assert.Equal(courierId, assignedEvent.CourierId);
    }

    [Fact]
    public void Create_WithEmptyOrderId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => DeliveryAssignment.Create(Guid.Empty, CourierId.New(), Now));
    }

    [Fact]
    public void FullLifecycle_PendingToCompleted_TransitionsInOrderAndRaisesCompletedEvent()
    {
        var assignment = DeliveryAssignment.Create(Guid.NewGuid(), CourierId.New(), Now);
        assignment.ClearDomainEvents();

        assignment.ConfirmPickup(Now);
        Assert.Equal(DeliveryAssignmentStatus.InTransit, assignment.Status);

        assignment.Complete(Now);

        Assert.Equal(DeliveryAssignmentStatus.Completed, assignment.Status);
        Assert.Equal(Now, assignment.CompletedAt);
        Assert.Contains(assignment.DomainEvents, e => e is DeliveryCompletedEvent);
    }

    [Fact]
    public void Complete_WithoutConfirmingPickup_ThrowsInvalidAssignmentStateTransitionException()
    {
        var assignment = DeliveryAssignment.Create(Guid.NewGuid(), CourierId.New(), Now);

        Assert.Throws<InvalidAssignmentStateTransitionException>(() => assignment.Complete(Now));
    }

    [Fact]
    public void ConfirmPickup_OnAlreadyCompletedAssignment_ThrowsInvalidAssignmentStateTransitionException()
    {
        var assignment = DeliveryAssignment.Create(Guid.NewGuid(), CourierId.New(), Now);
        assignment.ConfirmPickup(Now);
        assignment.Complete(Now);

        Assert.Throws<InvalidAssignmentStateTransitionException>(() => assignment.ConfirmPickup(Now));
    }
}

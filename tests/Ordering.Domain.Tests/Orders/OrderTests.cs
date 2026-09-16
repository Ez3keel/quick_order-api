using Ordering.Domain.Common;
using Ordering.Domain.Orders;
using Ordering.Domain.Orders.Events;
using Ordering.Domain.Orders.Exceptions;

namespace Ordering.Domain.Tests.Orders;

public class OrderTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 16, 12, 0, 0, TimeSpan.Zero);

    private static OrderItem SampleItem(int quantity = 2) =>
        new(Guid.NewGuid(), "X-Burger", new Money(19.90m), quantity);

    private static Order PlaceSampleOrder(int quantity = 2) =>
        Order.Place(Guid.NewGuid(), Guid.NewGuid(), [SampleItem(quantity)], Now);

    [Fact]
    public void Place_WithValidData_CreatesOrderInReceivedStatus()
    {
        var order = PlaceSampleOrder();

        Assert.Equal(OrderStatus.Received, order.Status);
        Assert.Single(order.Items);
        Assert.Equal(Now, order.CreatedAt);
    }

    [Fact]
    public void Place_WithValidData_RaisesOrderPlacedEvent()
    {
        var order = PlaceSampleOrder();

        var domainEvent = Assert.Single(order.DomainEvents);
        var placedEvent = Assert.IsType<OrderPlacedEvent>(domainEvent);
        Assert.Equal(order.Id, placedEvent.OrderId);
    }

    [Fact]
    public void Place_WithNoItems_ThrowsEmptyOrderException()
    {
        Assert.Throws<EmptyOrderException>(() =>
            Order.Place(Guid.NewGuid(), Guid.NewGuid(), [], Now));
    }

    [Fact]
    public void Place_WithEmptyCustomerId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            Order.Place(Guid.Empty, Guid.NewGuid(), [SampleItem()], Now));
    }

    [Fact]
    public void TotalAmount_SumsAllItemSubtotals()
    {
        var order = Order.Place(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [
                new OrderItem(Guid.NewGuid(), "X-Burger", new Money(19.90m), 2),
                new OrderItem(Guid.NewGuid(), "Batata Frita", new Money(12.00m), 1),
            ],
            Now);

        Assert.Equal(new Money(51.80m), order.TotalAmount);
    }

    [Fact]
    public void FullLifecycle_ReceivedToDelivered_TransitionsInOrder()
    {
        var order = PlaceSampleOrder();

        order.StartPreparing(Now);
        Assert.Equal(OrderStatus.Preparing, order.Status);

        order.MarkReadyForAssignment(Now);
        Assert.Equal(OrderStatus.AwaitingCourier, order.Status);
        Assert.Contains(order.DomainEvents, e => e is OrderReadyForAssignmentEvent);

        order.DispatchForDelivery(Now);
        Assert.Equal(OrderStatus.OutForDelivery, order.Status);

        order.MarkDelivered(Now);
        Assert.Equal(OrderStatus.Delivered, order.Status);
    }

    [Theory]
    [InlineData(OrderStatus.Received)]
    [InlineData(OrderStatus.Preparing)]
    [InlineData(OrderStatus.AwaitingCourier)]
    public void Cancel_BeforeCourierPickup_IsAllowed(OrderStatus statusBeforeCancel)
    {
        var order = PlaceSampleOrder();
        AdvanceTo(order, statusBeforeCancel);

        order.Cancel(Now);

        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Cancel_AfterDispatchedForDelivery_ThrowsInvalidOrderStateTransitionException()
    {
        var order = PlaceSampleOrder();
        AdvanceTo(order, OrderStatus.OutForDelivery);

        var exception = Assert.Throws<InvalidOrderStateTransitionException>(() => order.Cancel(Now));

        Assert.Equal(OrderStatus.OutForDelivery, exception.From);
        Assert.Equal(OrderStatus.Cancelled, exception.To);
    }

    [Fact]
    public void MarkDelivered_WithoutBeingDispatched_ThrowsInvalidOrderStateTransitionException()
    {
        var order = PlaceSampleOrder();

        Assert.Throws<InvalidOrderStateTransitionException>(() => order.MarkDelivered(Now));
    }

    [Fact]
    public void StartPreparing_OnAlreadyDeliveredOrder_ThrowsInvalidOrderStateTransitionException()
    {
        var order = PlaceSampleOrder();
        AdvanceTo(order, OrderStatus.Delivered);

        Assert.Throws<InvalidOrderStateTransitionException>(() => order.StartPreparing(Now));
    }

    private static void AdvanceTo(Order order, OrderStatus target)
    {
        if (target is OrderStatus.Received) return;

        order.StartPreparing(Now);
        if (target is OrderStatus.Preparing) return;

        order.MarkReadyForAssignment(Now);
        if (target is OrderStatus.AwaitingCourier) return;

        order.DispatchForDelivery(Now);
        if (target is OrderStatus.OutForDelivery) return;

        order.MarkDelivered(Now);
    }
}

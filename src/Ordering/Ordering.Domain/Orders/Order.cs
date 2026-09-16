using Ordering.Domain.Common;
using Ordering.Domain.Orders.Events;
using Ordering.Domain.Orders.Exceptions;

namespace Ordering.Domain.Orders;

public sealed class Order : AggregateRoot<OrderId>
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> AllowedTransitions = new()
    {
        [OrderStatus.Received] = [OrderStatus.Preparing, OrderStatus.Cancelled],
        [OrderStatus.Preparing] = [OrderStatus.AwaitingCourier, OrderStatus.Cancelled],
        [OrderStatus.AwaitingCourier] = [OrderStatus.OutForDelivery, OrderStatus.Cancelled],
        [OrderStatus.OutForDelivery] = [OrderStatus.Delivered],
        [OrderStatus.Delivered] = [],
        [OrderStatus.Cancelled] = [],
    };

    private readonly List<OrderItem> _items = [];

    public Guid CustomerId { get; private init; }
    public Guid RestaurantId { get; private init; }
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    public OrderStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public Money TotalAmount => _items
        .Select(i => i.Subtotal)
        .Aggregate(Money.Zero(), (acc, subtotal) => acc + subtotal);

    private Order() { }

    private Order(OrderId id, Guid customerId, Guid restaurantId, IEnumerable<OrderItem> items, DateTimeOffset now)
        : base(id)
    {
        CustomerId = customerId;
        RestaurantId = restaurantId;
        _items.AddRange(items);
        Status = OrderStatus.Received;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public static Order Place(Guid customerId, Guid restaurantId, IReadOnlyCollection<OrderItem> items, DateTimeOffset now)
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException("CustomerId is required.", nameof(customerId));

        if (restaurantId == Guid.Empty)
            throw new ArgumentException("RestaurantId is required.", nameof(restaurantId));

        if (items is null || items.Count == 0)
            throw new EmptyOrderException();

        var order = new Order(OrderId.New(), customerId, restaurantId, items, now);
        order.Raise(new OrderPlacedEvent(order.Id, customerId, restaurantId, now));
        return order;
    }

    public void StartPreparing(DateTimeOffset now) => TransitionTo(OrderStatus.Preparing, now);

    public void MarkReadyForAssignment(DateTimeOffset now)
    {
        TransitionTo(OrderStatus.AwaitingCourier, now);
        Raise(new OrderReadyForAssignmentEvent(Id, RestaurantId, now));
    }

    public void DispatchForDelivery(DateTimeOffset now) => TransitionTo(OrderStatus.OutForDelivery, now);

    public void MarkDelivered(DateTimeOffset now) => TransitionTo(OrderStatus.Delivered, now);

    public void Cancel(DateTimeOffset now) => TransitionTo(OrderStatus.Cancelled, now);

    private void TransitionTo(OrderStatus next, DateTimeOffset now)
    {
        if (!AllowedTransitions[Status].Contains(next))
            throw new InvalidOrderStateTransitionException(Status, next);

        var previous = Status;
        Status = next;
        UpdatedAt = now;
        Raise(new OrderStatusChangedEvent(Id, previous, next, now));
    }
}

namespace Ordering.Domain.Orders;

/// <summary>
/// Received: order was placed and paid/validated, not yet accepted by the restaurant.
/// Preparing: restaurant accepted and is cooking.
/// AwaitingCourier: food is ready, order is queued for the Delivery Assignment Worker to pick a courier.
/// OutForDelivery: a courier was assigned and picked up the order.
/// Delivered / Cancelled: terminal states.
/// </summary>
public enum OrderStatus
{
    Received = 0,
    Preparing = 1,
    AwaitingCourier = 2,
    OutForDelivery = 3,
    Delivered = 4,
    Cancelled = 5,
}

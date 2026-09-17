namespace Ordering.Api.Contracts;

/// <summary>No CustomerId here — it comes from the caller's JWT (NameIdentifier
/// claim), never from the request body, so one customer can't place an order as
/// another by editing the payload.</summary>
public sealed record PlaceOrderRequest(
    Guid RestaurantId,
    IReadOnlyCollection<PlaceOrderRequestItem> Items);

public sealed record PlaceOrderRequestItem(Guid MenuItemId, int Quantity);

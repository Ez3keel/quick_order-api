namespace Ordering.Api.Contracts;

public sealed record PlaceOrderRequest(
    Guid CustomerId,
    Guid RestaurantId,
    IReadOnlyCollection<PlaceOrderRequestItem> Items);

public sealed record PlaceOrderRequestItem(Guid MenuItemId, int Quantity);

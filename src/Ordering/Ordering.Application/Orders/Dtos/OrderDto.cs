namespace Ordering.Application.Orders.Dtos;

public sealed record OrderDto(
    Guid Id,
    Guid CustomerId,
    Guid RestaurantId,
    string Status,
    decimal TotalAmount,
    string Currency,
    IReadOnlyCollection<OrderItemDto> Items,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record OrderItemDto(
    Guid MenuItemId,
    string ProductName,
    decimal UnitPrice,
    string Currency,
    int Quantity);

using Ordering.Domain.Orders;

namespace Ordering.Application.Orders.Dtos;

public static class OrderMappingExtensions
{
    public static OrderDto ToDto(this Order order) => new(
        order.Id.Value,
        order.CustomerId,
        order.RestaurantId,
        order.Status.ToString(),
        order.TotalAmount.Amount,
        order.TotalAmount.Currency,
        order.Items.Select(i => new OrderItemDto(
            i.MenuItemId,
            i.ProductName,
            i.UnitPrice.Amount,
            i.UnitPrice.Currency,
            i.Quantity)).ToList(),
        order.CreatedAt,
        order.UpdatedAt);
}

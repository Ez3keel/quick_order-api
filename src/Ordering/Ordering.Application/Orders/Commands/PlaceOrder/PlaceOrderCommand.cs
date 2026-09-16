using Ordering.Application.Orders.Dtos;
using MediatR;

namespace Ordering.Application.Orders.Commands.PlaceOrder;

public sealed record PlaceOrderCommand(
    Guid CustomerId,
    Guid RestaurantId,
    IReadOnlyCollection<PlaceOrderItem> Items) : IRequest<OrderDto>;

public sealed record PlaceOrderItem(Guid MenuItemId, int Quantity);

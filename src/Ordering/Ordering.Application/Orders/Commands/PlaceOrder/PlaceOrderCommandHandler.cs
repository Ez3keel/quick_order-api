using MediatR;
using Ordering.Application.Abstractions;
using Ordering.Application.Orders.Dtos;
using Ordering.Application.Orders.Exceptions;
using Ordering.Domain.Common;
using Ordering.Domain.Orders;

namespace Ordering.Application.Orders.Commands.PlaceOrder;

public sealed class PlaceOrderCommandHandler(
    ICatalogClient catalogClient,
    IOrderRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<PlaceOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        var restaurant = await catalogClient.GetRestaurantAsync(request.RestaurantId, cancellationToken)
            ?? throw new RestaurantUnavailableException(request.RestaurantId);

        if (!restaurant.IsOpen)
            throw new RestaurantUnavailableException(request.RestaurantId);

        var menuById = restaurant.Menu.ToDictionary(m => m.Id);

        var items = request.Items.Select(requested =>
        {
            if (!menuById.TryGetValue(requested.MenuItemId, out var menuItem) || !menuItem.IsAvailable)
                throw new MenuItemUnavailableException(requested.MenuItemId);

            return new OrderItem(menuItem.Id, menuItem.Name, new Money(menuItem.Price, menuItem.Currency), requested.Quantity);
        }).ToList();

        var now = timeProvider.GetUtcNow();
        var order = Order.Place(request.CustomerId, request.RestaurantId, items, now);

        repository.Add(order);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return order.ToDto();
    }
}

using Catalog.Application.Abstractions;
using Catalog.Application.Restaurants.Dtos;
using Catalog.Application.Restaurants.Exceptions;
using Catalog.Domain.Common;
using Catalog.Domain.Restaurants;
using MediatR;

namespace Catalog.Application.Restaurants.Commands.AddMenuItem;

public sealed class AddMenuItemCommandHandler(
    IRestaurantRepository repository,
    IUnitOfWork unitOfWork,
    IMenuCache menuCache) : IRequestHandler<AddMenuItemCommand, MenuItemDto>
{
    public async Task<MenuItemDto> Handle(AddMenuItemCommand request, CancellationToken cancellationToken)
    {
        var restaurant = await repository.GetByIdAsync(RestaurantId.From(request.RestaurantId), cancellationToken)
            ?? throw new RestaurantNotFoundException(request.RestaurantId);

        var item = restaurant.AddMenuItem(request.ProductName, new Money(request.Price, request.Currency));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await menuCache.InvalidateAsync(request.RestaurantId, cancellationToken);

        return new MenuItemDto(item.Id.Value, item.Name, item.Price.Amount, item.Price.Currency, item.IsAvailable);
    }
}

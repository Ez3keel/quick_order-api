using Catalog.Application.Abstractions;
using Catalog.Application.Restaurants.Exceptions;
using Catalog.Domain.Common;
using Catalog.Domain.Restaurants;
using MediatR;

namespace Catalog.Application.Restaurants.Commands.ChangeMenuItemPrice;

public sealed class ChangeMenuItemPriceCommandHandler(
    IRestaurantRepository repository,
    IUnitOfWork unitOfWork,
    IMenuCache menuCache,
    TimeProvider timeProvider) : IRequestHandler<ChangeMenuItemPriceCommand>
{
    public async Task Handle(ChangeMenuItemPriceCommand request, CancellationToken cancellationToken)
    {
        var restaurant = await repository.GetByIdAsync(RestaurantId.From(request.RestaurantId), cancellationToken)
            ?? throw new RestaurantNotFoundException(request.RestaurantId);

        restaurant.ChangePrice(
            MenuItemId.From(request.MenuItemId),
            new Money(request.NewPrice, request.Currency),
            timeProvider.GetUtcNow());

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await menuCache.InvalidateAsync(request.RestaurantId, cancellationToken);
    }
}

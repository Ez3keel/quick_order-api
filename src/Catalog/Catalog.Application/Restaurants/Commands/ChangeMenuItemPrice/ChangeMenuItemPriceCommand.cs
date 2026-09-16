using MediatR;

namespace Catalog.Application.Restaurants.Commands.ChangeMenuItemPrice;

public sealed record ChangeMenuItemPriceCommand(
    Guid RestaurantId,
    Guid MenuItemId,
    decimal NewPrice,
    string Currency) : IRequest;

using Catalog.Application.Restaurants.Dtos;
using MediatR;

namespace Catalog.Application.Restaurants.Commands.AddMenuItem;

public sealed record AddMenuItemCommand(
    Guid RestaurantId,
    string ProductName,
    decimal Price,
    string Currency) : IRequest<MenuItemDto>;

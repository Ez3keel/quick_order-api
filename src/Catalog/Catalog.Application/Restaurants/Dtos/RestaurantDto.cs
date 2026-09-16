namespace Catalog.Application.Restaurants.Dtos;

public sealed record RestaurantDto(
    Guid Id,
    string Name,
    bool IsOpen,
    IReadOnlyCollection<MenuItemDto> Menu);

public sealed record MenuItemDto(
    Guid Id,
    string Name,
    decimal Price,
    string Currency,
    bool IsAvailable);

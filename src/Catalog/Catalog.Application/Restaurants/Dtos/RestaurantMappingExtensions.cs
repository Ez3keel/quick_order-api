using Catalog.Domain.Restaurants;

namespace Catalog.Application.Restaurants.Dtos;

public static class RestaurantMappingExtensions
{
    public static RestaurantDto ToDto(this Restaurant restaurant) => new(
        restaurant.Id.Value,
        restaurant.Name,
        restaurant.IsOpen,
        restaurant.Menu.Select(item => new MenuItemDto(
            item.Id.Value,
            item.Name,
            item.Price.Amount,
            item.Price.Currency,
            item.IsAvailable)).ToList());
}

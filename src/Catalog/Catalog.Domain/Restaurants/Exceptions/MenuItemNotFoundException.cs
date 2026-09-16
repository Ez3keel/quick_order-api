namespace Catalog.Domain.Restaurants.Exceptions;

public sealed class MenuItemNotFoundException : Exception
{
    public MenuItemId MenuItemId { get; }

    public MenuItemNotFoundException(MenuItemId menuItemId)
        : base($"Menu item '{menuItemId}' was not found in this restaurant.")
    {
        MenuItemId = menuItemId;
    }
}

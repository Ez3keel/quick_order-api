namespace Ordering.Application.Orders.Exceptions;

/// <summary>Raised when placing an order references a menu item Catalog doesn't have,
/// or one that's currently marked unavailable.</summary>
public sealed class MenuItemUnavailableException : Exception
{
    public Guid MenuItemId { get; }

    public MenuItemUnavailableException(Guid menuItemId)
        : base($"Menu item '{menuItemId}' is not available to order right now.")
    {
        MenuItemId = menuItemId;
    }
}

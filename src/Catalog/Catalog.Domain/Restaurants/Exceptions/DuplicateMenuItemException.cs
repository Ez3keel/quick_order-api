namespace Catalog.Domain.Restaurants.Exceptions;

public sealed class DuplicateMenuItemException : Exception
{
    public string ProductName { get; }

    public DuplicateMenuItemException(string productName)
        : base($"A menu item named '{productName}' already exists for this restaurant.")
    {
        ProductName = productName;
    }
}

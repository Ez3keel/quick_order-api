using Catalog.Domain.Common;
using Catalog.Domain.Restaurants.Events;
using Catalog.Domain.Restaurants.Exceptions;

namespace Catalog.Domain.Restaurants;

public sealed class Restaurant : AggregateRoot<RestaurantId>
{
    private readonly List<MenuItem> _menu = [];

    public string Name { get; private set; }
    public bool IsOpen { get; private set; }
    public IReadOnlyCollection<MenuItem> Menu => _menu.AsReadOnly();

    private Restaurant() => Name = null!;

    private Restaurant(RestaurantId id, string name) : base(id)
    {
        Name = name;
        IsOpen = false;
    }

    public static Restaurant Register(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Restaurant name is required.", nameof(name));

        return new Restaurant(RestaurantId.New(), name);
    }

    public void Open() => IsOpen = true;

    public void Close() => IsOpen = false;

    public MenuItem AddMenuItem(string productName, Money price)
    {
        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("Product name is required.", nameof(productName));

        if (_menu.Any(i => i.Name.Equals(productName, StringComparison.OrdinalIgnoreCase)))
            throw new DuplicateMenuItemException(productName);

        var item = new MenuItem(MenuItemId.New(), productName, price);
        _menu.Add(item);
        return item;
    }

    public void ChangePrice(MenuItemId menuItemId, Money newPrice, DateTimeOffset now)
    {
        var item = FindItemOrThrow(menuItemId);
        var previousPrice = item.Price;

        item.ChangePrice(newPrice);

        Raise(new MenuItemPriceChangedEvent(Id, menuItemId, previousPrice, newPrice, now));
    }

    public void SetItemAvailability(MenuItemId menuItemId, bool isAvailable)
    {
        var item = FindItemOrThrow(menuItemId);
        item.SetAvailability(isAvailable);
    }

    private MenuItem FindItemOrThrow(MenuItemId menuItemId) =>
        _menu.FirstOrDefault(i => i.Id == menuItemId)
        ?? throw new MenuItemNotFoundException(menuItemId);
}

using Catalog.Domain.Common;

namespace Catalog.Domain.Restaurants;

/// <summary>
/// Child entity of Restaurant. Mutated only through the Restaurant aggregate root
/// (constructor and mutators are internal) so every change goes through its invariants.
/// </summary>
public sealed class MenuItem : Entity<MenuItemId>
{
    public string Name { get; private set; }
    public Money Price { get; private set; }
    public bool IsAvailable { get; private set; }

    internal MenuItem(MenuItemId id, string name, Money price) : base(id)
    {
        Name = name;
        Price = price;
        IsAvailable = true;
    }

    internal void ChangePrice(Money newPrice) => Price = newPrice;

    internal void SetAvailability(bool isAvailable) => IsAvailable = isAvailable;
}

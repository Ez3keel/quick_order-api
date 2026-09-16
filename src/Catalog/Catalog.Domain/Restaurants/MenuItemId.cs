namespace Catalog.Domain.Restaurants;

public readonly record struct MenuItemId(Guid Value)
{
    public static MenuItemId New() => new(Guid.NewGuid());

    public static MenuItemId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}

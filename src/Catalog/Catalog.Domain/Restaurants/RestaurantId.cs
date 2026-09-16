namespace Catalog.Domain.Restaurants;

public readonly record struct RestaurantId(Guid Value)
{
    public static RestaurantId New() => new(Guid.NewGuid());

    public static RestaurantId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}

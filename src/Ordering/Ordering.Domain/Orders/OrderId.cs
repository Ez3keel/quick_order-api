namespace Ordering.Domain.Orders;

public readonly record struct OrderId(Guid Value)
{
    public static OrderId New() => new(Guid.NewGuid());

    public static OrderId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}

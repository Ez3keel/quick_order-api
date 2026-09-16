namespace Delivery.Domain.Couriers;

public readonly record struct CourierId(Guid Value)
{
    public static CourierId New() => new(Guid.NewGuid());

    public static CourierId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}

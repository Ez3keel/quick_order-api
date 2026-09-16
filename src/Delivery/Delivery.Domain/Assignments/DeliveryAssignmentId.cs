namespace Delivery.Domain.Assignments;

public readonly record struct DeliveryAssignmentId(Guid Value)
{
    public static DeliveryAssignmentId New() => new(Guid.NewGuid());

    public static DeliveryAssignmentId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}

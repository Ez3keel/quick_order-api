namespace Delivery.Application.Couriers.Exceptions;

public sealed class CourierNotFoundException : Exception
{
    public Guid CourierId { get; }

    public CourierNotFoundException(Guid courierId) : base($"Courier '{courierId}' was not found.")
    {
        CourierId = courierId;
    }
}

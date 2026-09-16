namespace Delivery.Domain.Couriers.Exceptions;

public sealed class CourierNotAvailableException : Exception
{
    public CourierId CourierId { get; }
    public CourierStatus CurrentStatus { get; }

    public CourierNotAvailableException(CourierId courierId, CourierStatus currentStatus)
        : base($"Courier '{courierId}' is not available for assignment (current status: {currentStatus}).")
    {
        CourierId = courierId;
        CurrentStatus = currentStatus;
    }
}

namespace Delivery.Domain.Couriers;

/// <summary>
/// Offline: not working right now, excluded from assignment candidates.
/// Available: online and free to be assigned a delivery.
/// Busy: currently carrying out a delivery, not a candidate for new assignments.
/// </summary>
public enum CourierStatus
{
    Offline = 0,
    Available = 1,
    Busy = 2,
}

using Delivery.Domain.Common;
using Delivery.Domain.Couriers.Exceptions;

namespace Delivery.Domain.Couriers;

public sealed class Courier : AggregateRoot<CourierId>
{
    public string Name { get; private set; }
    public CourierStatus Status { get; private set; }
    public GeoCoordinates? CurrentLocation { get; private set; }

    private Courier() => Name = null!;

    private Courier(CourierId id, string name) : base(id)
    {
        Name = name;
        Status = CourierStatus.Offline;
    }

    public static Courier Register(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Courier name is required.", nameof(name));

        return new Courier(CourierId.New(), name);
    }

    public void GoOnline(GeoCoordinates location)
    {
        Status = CourierStatus.Available;
        CurrentLocation = location;
    }

    public void GoOffline()
    {
        if (Status == CourierStatus.Busy)
            throw new CourierNotAvailableException(Id, Status);

        Status = CourierStatus.Offline;
    }

    public void UpdateLocation(GeoCoordinates location)
    {
        if (Status == CourierStatus.Offline)
            throw new CourierNotAvailableException(Id, Status);

        CurrentLocation = location;
    }

    /// <summary>Reserves the courier for a delivery. Called by the assignment worker's
    /// single consumer, so no concurrent callers compete for the same courier in-process.</summary>
    public void Reserve()
    {
        if (Status != CourierStatus.Available)
            throw new CourierNotAvailableException(Id, Status);

        Status = CourierStatus.Busy;
    }

    public void Release()
    {
        if (Status != CourierStatus.Busy)
            throw new CourierNotAvailableException(Id, Status);

        Status = CourierStatus.Available;
    }
}

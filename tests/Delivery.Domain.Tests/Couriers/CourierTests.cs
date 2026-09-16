using Delivery.Domain.Common;
using Delivery.Domain.Couriers;
using Delivery.Domain.Couriers.Exceptions;

namespace Delivery.Domain.Tests.Couriers;

public class CourierTests
{
    private static readonly GeoCoordinates SampleLocation = new(-23.5505, -46.6333);

    [Fact]
    public void Register_CreatesOfflineCourier()
    {
        var courier = Courier.Register("João");

        Assert.Equal(CourierStatus.Offline, courier.Status);
        Assert.Null(courier.CurrentLocation);
    }

    [Fact]
    public void GoOnline_SetsStatusAvailableAndLocation()
    {
        var courier = Courier.Register("João");

        courier.GoOnline(SampleLocation);

        Assert.Equal(CourierStatus.Available, courier.Status);
        Assert.Equal(SampleLocation, courier.CurrentLocation);
    }

    [Fact]
    public void Reserve_WhenAvailable_MarksBusy()
    {
        var courier = Courier.Register("João");
        courier.GoOnline(SampleLocation);

        courier.Reserve();

        Assert.Equal(CourierStatus.Busy, courier.Status);
    }

    [Fact]
    public void Reserve_WhenOffline_ThrowsCourierNotAvailableException()
    {
        var courier = Courier.Register("João");

        Assert.Throws<CourierNotAvailableException>(courier.Reserve);
    }

    [Fact]
    public void Reserve_WhenAlreadyBusy_ThrowsCourierNotAvailableException()
    {
        var courier = Courier.Register("João");
        courier.GoOnline(SampleLocation);
        courier.Reserve();

        Assert.Throws<CourierNotAvailableException>(courier.Reserve);
    }

    [Fact]
    public void Release_WhenBusy_MarksAvailableAgain()
    {
        var courier = Courier.Register("João");
        courier.GoOnline(SampleLocation);
        courier.Reserve();

        courier.Release();

        Assert.Equal(CourierStatus.Available, courier.Status);
    }

    [Fact]
    public void GoOffline_WhenBusy_ThrowsCourierNotAvailableException()
    {
        var courier = Courier.Register("João");
        courier.GoOnline(SampleLocation);
        courier.Reserve();

        Assert.Throws<CourierNotAvailableException>(courier.GoOffline);
    }

    [Fact]
    public void UpdateLocation_WhenOffline_ThrowsCourierNotAvailableException()
    {
        var courier = Courier.Register("João");

        Assert.Throws<CourierNotAvailableException>(() => courier.UpdateLocation(SampleLocation));
    }

    [Theory]
    [InlineData(-91, 0)]
    [InlineData(91, 0)]
    [InlineData(0, -181)]
    [InlineData(0, 181)]
    public void GeoCoordinates_OutOfRange_ThrowsArgumentOutOfRangeException(double lat, double lng)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GeoCoordinates(lat, lng));
    }
}

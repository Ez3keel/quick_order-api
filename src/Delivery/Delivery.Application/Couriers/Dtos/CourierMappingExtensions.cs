using Delivery.Domain.Couriers;

namespace Delivery.Application.Couriers.Dtos;

public static class CourierMappingExtensions
{
    public static CourierDto ToDto(this Courier courier) => new(
        courier.Id.Value,
        courier.Name,
        courier.Status.ToString(),
        courier.CurrentLocation?.Latitude,
        courier.CurrentLocation?.Longitude);
}

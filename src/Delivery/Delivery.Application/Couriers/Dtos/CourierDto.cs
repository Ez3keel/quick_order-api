namespace Delivery.Application.Couriers.Dtos;

public sealed record CourierDto(
    Guid Id,
    string Name,
    string Status,
    double? Latitude,
    double? Longitude);

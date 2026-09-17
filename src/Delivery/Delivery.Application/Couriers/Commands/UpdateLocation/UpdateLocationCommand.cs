using MediatR;

namespace Delivery.Application.Couriers.Commands.UpdateLocation;

public sealed record UpdateLocationCommand(Guid CourierId, double Latitude, double Longitude) : IRequest;

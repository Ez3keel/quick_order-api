using MediatR;

namespace Delivery.Application.Couriers.Commands.GoOnline;

public sealed record GoOnlineCommand(Guid CourierId, double Latitude, double Longitude) : IRequest;

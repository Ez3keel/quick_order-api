using MediatR;

namespace Delivery.Application.Couriers.Commands.GoOffline;

public sealed record GoOfflineCommand(Guid CourierId) : IRequest;

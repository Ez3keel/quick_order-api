using MediatR;

namespace Ordering.Application.Orders.Commands.DispatchForDelivery;

public sealed record DispatchForDeliveryCommand(Guid OrderId) : IRequest;

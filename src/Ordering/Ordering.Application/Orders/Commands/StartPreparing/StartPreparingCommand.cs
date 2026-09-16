using MediatR;

namespace Ordering.Application.Orders.Commands.StartPreparing;

public sealed record StartPreparingCommand(Guid OrderId) : IRequest;

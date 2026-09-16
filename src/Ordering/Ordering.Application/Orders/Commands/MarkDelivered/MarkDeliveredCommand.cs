using MediatR;

namespace Ordering.Application.Orders.Commands.MarkDelivered;

public sealed record MarkDeliveredCommand(Guid OrderId) : IRequest;

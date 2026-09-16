using MediatR;

namespace Ordering.Application.Orders.Commands.MarkReadyForAssignment;

public sealed record MarkReadyForAssignmentCommand(Guid OrderId) : IRequest;

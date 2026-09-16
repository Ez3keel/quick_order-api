using MediatR;
using Ordering.Application.Abstractions;
using Ordering.Application.Orders.Exceptions;
using Ordering.Domain.Orders;

namespace Ordering.Application.Orders.Commands.MarkReadyForAssignment;

/// <summary>Marks the order AwaitingCourier and raises OrderReadyForAssignmentEvent —
/// once the Outbox exists (Fase 3), this is what the Delivery Assignment Worker's
/// queue consumer reacts to.</summary>
public sealed class MarkReadyForAssignmentCommandHandler(
    IOrderRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<MarkReadyForAssignmentCommand>
{
    public async Task Handle(MarkReadyForAssignmentCommand request, CancellationToken cancellationToken)
    {
        var order = await repository.GetByIdAsync(OrderId.From(request.OrderId), cancellationToken)
            ?? throw new OrderNotFoundException(request.OrderId);

        order.MarkReadyForAssignment(timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

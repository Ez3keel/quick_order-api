using MediatR;
using Ordering.Application.Abstractions;
using Ordering.Application.Orders.Exceptions;
using Ordering.Domain.Orders;

namespace Ordering.Application.Orders.Commands.MarkDelivered;

public sealed class MarkDeliveredCommandHandler(
    IOrderRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<MarkDeliveredCommand>
{
    public async Task Handle(MarkDeliveredCommand request, CancellationToken cancellationToken)
    {
        var order = await repository.GetByIdAsync(OrderId.From(request.OrderId), cancellationToken)
            ?? throw new OrderNotFoundException(request.OrderId);

        order.MarkDelivered(timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

using MediatR;
using Ordering.Application.Abstractions;
using Ordering.Application.Orders.Exceptions;
using Ordering.Domain.Orders;

namespace Ordering.Application.Orders.Commands.CancelOrder;

public sealed class CancelOrderCommandHandler(
    IOrderRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<CancelOrderCommand>
{
    public async Task Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await repository.GetByIdAsync(OrderId.From(request.OrderId), cancellationToken)
            ?? throw new OrderNotFoundException(request.OrderId);

        order.Cancel(timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

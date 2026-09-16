using MediatR;
using Ordering.Application.Abstractions;
using Ordering.Application.Orders.Exceptions;
using Ordering.Domain.Orders;

namespace Ordering.Application.Orders.Commands.StartPreparing;

public sealed class StartPreparingCommandHandler(
    IOrderRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<StartPreparingCommand>
{
    public async Task Handle(StartPreparingCommand request, CancellationToken cancellationToken)
    {
        var order = await repository.GetByIdAsync(OrderId.From(request.OrderId), cancellationToken)
            ?? throw new OrderNotFoundException(request.OrderId);

        order.StartPreparing(timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

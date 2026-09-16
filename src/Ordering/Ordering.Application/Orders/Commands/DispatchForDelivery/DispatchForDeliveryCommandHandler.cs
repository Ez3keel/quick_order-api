using MediatR;
using Ordering.Application.Abstractions;
using Ordering.Application.Orders.Exceptions;
using Ordering.Domain.Orders;

namespace Ordering.Application.Orders.Commands.DispatchForDelivery;

/// <summary>Called when the Delivery Assignment Worker successfully pairs the order
/// with a courier (today: a direct call from the API surface; once the message bus
/// exists, this will be triggered by consuming CourierAssignedEvent instead).</summary>
public sealed class DispatchForDeliveryCommandHandler(
    IOrderRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<DispatchForDeliveryCommand>
{
    public async Task Handle(DispatchForDeliveryCommand request, CancellationToken cancellationToken)
    {
        var order = await repository.GetByIdAsync(OrderId.From(request.OrderId), cancellationToken)
            ?? throw new OrderNotFoundException(request.OrderId);

        order.DispatchForDelivery(timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

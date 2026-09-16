using MediatR;
using Ordering.Application.Abstractions;
using Ordering.Application.Orders.Dtos;
using Ordering.Domain.Orders;

namespace Ordering.Application.Orders.Queries.GetOrderById;

public sealed class GetOrderByIdQueryHandler(IOrderRepository repository)
    : IRequestHandler<GetOrderByIdQuery, OrderDto?>
{
    public async Task<OrderDto?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await repository.GetByIdAsync(OrderId.From(request.OrderId), cancellationToken);
        return order?.ToDto();
    }
}

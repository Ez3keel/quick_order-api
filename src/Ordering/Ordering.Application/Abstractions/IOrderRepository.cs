using Ordering.Domain.Orders;

namespace Ordering.Application.Abstractions;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(OrderId id, CancellationToken cancellationToken);

    void Add(Order order);
}

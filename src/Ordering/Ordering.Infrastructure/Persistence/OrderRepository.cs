using Microsoft.EntityFrameworkCore;
using Ordering.Application.Abstractions;
using Ordering.Domain.Orders;

namespace Ordering.Infrastructure.Persistence;

public sealed class OrderRepository(OrderingDbContext dbContext) : IOrderRepository
{
    public Task<Order?> GetByIdAsync(OrderId id, CancellationToken cancellationToken) =>
        dbContext.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public void Add(Order order) => dbContext.Orders.Add(order);
}

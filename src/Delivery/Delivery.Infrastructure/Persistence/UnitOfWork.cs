using Delivery.Application.Abstractions;

namespace Delivery.Infrastructure.Persistence;

public sealed class UnitOfWork(DeliveryDbContext dbContext) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}

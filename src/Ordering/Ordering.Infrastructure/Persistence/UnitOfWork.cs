using Ordering.Application.Abstractions;

namespace Ordering.Infrastructure.Persistence;

public sealed class UnitOfWork(OrderingDbContext dbContext) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}

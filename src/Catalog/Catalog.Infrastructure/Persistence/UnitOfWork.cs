using Catalog.Application.Abstractions;

namespace Catalog.Infrastructure.Persistence;

public sealed class UnitOfWork(CatalogDbContext dbContext) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}

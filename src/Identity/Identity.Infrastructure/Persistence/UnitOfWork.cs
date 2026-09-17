using Identity.Application.Abstractions;

namespace Identity.Infrastructure.Persistence;

public sealed class UnitOfWork(IdentityDbContext dbContext) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}

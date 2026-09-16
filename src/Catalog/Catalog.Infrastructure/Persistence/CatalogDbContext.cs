using Catalog.Domain.Common;
using Catalog.Domain.Restaurants;
using Catalog.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Restaurant> Restaurants => Set<Restaurant>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("catalog");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
    }

    /// <summary>
    /// Drains domain events off every tracked aggregate and writes them as Outbox rows
    /// in the SAME SaveChanges call that persists the aggregate — EF Core wraps one
    /// SaveChangesAsync in a single transaction, so the aggregate's new state and its
    /// outbound event are atomic: either both land, or neither does.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var aggregatesWithEvents = ChangeTracker.Entries<IHasDomainEvents>()
            .Select(entry => entry.Entity)
            .Where(aggregate => aggregate.DomainEvents.Count > 0)
            .ToList();

        foreach (var aggregate in aggregatesWithEvents)
        {
            foreach (var domainEvent in aggregate.DomainEvents)
            {
                var outboxMessage = CatalogIntegrationEventMapper.TryMapToOutboxMessage(domainEvent);
                if (outboxMessage is not null)
                    OutboxMessages.Add(outboxMessage);
            }

            aggregate.ClearDomainEvents();
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}

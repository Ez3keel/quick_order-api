using Microsoft.EntityFrameworkCore;
using Ordering.Domain.Common;
using Ordering.Domain.Orders;
using Ordering.Infrastructure.Outbox;

namespace Ordering.Infrastructure.Persistence;

public sealed class OrderingDbContext(DbContextOptions<OrderingDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("ordering");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderingDbContext).Assembly);
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
                var outboxMessage = OrderingIntegrationEventMapper.TryMapToOutboxMessage(domainEvent);
                if (outboxMessage is not null)
                    OutboxMessages.Add(outboxMessage);
            }

            aggregate.ClearDomainEvents();
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}

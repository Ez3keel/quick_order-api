using Delivery.Domain.Assignments;
using Delivery.Domain.Common;
using Delivery.Domain.Couriers;
using Delivery.Infrastructure.Messaging;
using Delivery.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Delivery.Infrastructure.Persistence;

public sealed class DeliveryDbContext(DbContextOptions<DeliveryDbContext> options) : DbContext(options)
{
    public DbSet<Courier> Couriers => Set<Courier>();
    public DbSet<DeliveryAssignment> Assignments => Set<DeliveryAssignment>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("delivery");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DeliveryDbContext).Assembly);
    }

    /// <summary>
    /// Drains domain events off every tracked aggregate and writes them as Outbox rows
    /// in the SAME SaveChanges call that persists the aggregate — atomic by virtue of
    /// EF Core wrapping one SaveChangesAsync in a single transaction.
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
                var outboxMessage = DeliveryIntegrationEventMapper.TryMapToOutboxMessage(domainEvent);
                if (outboxMessage is not null)
                    OutboxMessages.Add(outboxMessage);
            }

            aggregate.ClearDomainEvents();
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}

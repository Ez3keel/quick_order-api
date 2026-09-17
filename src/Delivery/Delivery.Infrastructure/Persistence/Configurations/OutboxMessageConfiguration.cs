using Delivery.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Delivery.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Type).IsRequired().HasMaxLength(200);
        builder.Property(m => m.Content).IsRequired();
        builder.Property(m => m.OccurredOnUtc).IsRequired();

        builder.HasIndex(m => m.ProcessedOnUtc);
    }
}

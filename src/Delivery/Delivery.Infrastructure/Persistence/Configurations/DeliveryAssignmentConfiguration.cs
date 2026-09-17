using Delivery.Domain.Assignments;
using Delivery.Domain.Couriers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Delivery.Infrastructure.Persistence.Configurations;

public sealed class DeliveryAssignmentConfiguration : IEntityTypeConfiguration<DeliveryAssignment>
{
    public void Configure(EntityTypeBuilder<DeliveryAssignment> builder)
    {
        builder.ToTable("DeliveryAssignments");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasConversion(id => id.Value, value => DeliveryAssignmentId.From(value))
            .ValueGeneratedNever();

        builder.Property(a => a.OrderId).IsRequired();

        builder.Property(a => a.CourierId)
            .HasConversion(id => id.Value, value => CourierId.From(value))
            .IsRequired();

        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.AssignedAt).IsRequired();
        builder.Property(a => a.CompletedAt);

        builder.HasIndex(a => a.OrderId);
    }
}

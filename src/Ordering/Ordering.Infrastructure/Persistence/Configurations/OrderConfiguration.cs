using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ordering.Domain.Orders;

namespace Ordering.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id)
            .HasConversion(id => id.Value, value => OrderId.From(value))
            .ValueGeneratedNever();

        builder.Property(o => o.CustomerId).IsRequired();
        builder.Property(o => o.RestaurantId).IsRequired();
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(o => o.CreatedAt).IsRequired();
        builder.Property(o => o.UpdatedAt).IsRequired();

        builder.Ignore(o => o.TotalAmount);
        builder.Ignore(o => o.DomainEvents);

        // OrderItem is mapped as its own entity (see OrderItemConfiguration), same
        // reasoning as Catalog.MenuItem: UnitPrice is a Money struct, and EF Core's
        // OwnsMany requires a reference-type dependent, so it can't nest a struct's
        // ComplexProperty inside an owned collection.
        builder.Metadata.FindNavigation(nameof(Order.Items))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey("OrderId")
            .IsRequired();
    }
}

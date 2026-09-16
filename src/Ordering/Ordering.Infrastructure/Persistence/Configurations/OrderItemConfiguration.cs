using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ordering.Domain.Orders;

namespace Ordering.Infrastructure.Persistence.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");

        // OrderItem is a value object in the domain (no identity of its own) — the
        // "Id" here is purely a storage-level surrogate key for the EF mapping.
        builder.Property<Guid>("Id").ValueGeneratedOnAdd();
        builder.HasKey("Id");

        builder.Property(i => i.MenuItemId).IsRequired();
        builder.Property(i => i.ProductName).IsRequired().HasMaxLength(200);
        builder.Property(i => i.Quantity).IsRequired();

        builder.ComplexProperty(i => i.UnitPrice, price =>
        {
            price.Property(p => p.Amount).HasColumnName("UnitPrice").HasPrecision(18, 2);
            price.Property(p => p.Currency).HasColumnName("Currency").HasMaxLength(3);
        });
    }
}

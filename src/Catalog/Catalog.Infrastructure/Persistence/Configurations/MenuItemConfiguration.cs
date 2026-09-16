using Catalog.Domain.Restaurants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence.Configurations;

public sealed class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.ToTable("MenuItems");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .HasConversion(id => id.Value, value => MenuItemId.From(value))
            .ValueGeneratedNever();

        builder.Property(m => m.Name).IsRequired().HasMaxLength(200);
        builder.Property(m => m.IsAvailable);

        builder.ComplexProperty(m => m.Price, price =>
        {
            price.Property(p => p.Amount).HasColumnName("Price").HasPrecision(18, 2);
            price.Property(p => p.Currency).HasColumnName("Currency").HasMaxLength(3);
        });
    }
}

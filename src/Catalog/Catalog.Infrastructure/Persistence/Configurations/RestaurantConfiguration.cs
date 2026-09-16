using Catalog.Domain.Restaurants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence.Configurations;

public sealed class RestaurantConfiguration : IEntityTypeConfiguration<Restaurant>
{
    public void Configure(EntityTypeBuilder<Restaurant> builder)
    {
        builder.ToTable("Restaurants");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasConversion(id => id.Value, value => RestaurantId.From(value))
            .ValueGeneratedNever();

        builder.Property(r => r.Name).IsRequired().HasMaxLength(200);
        builder.Property(r => r.IsOpen);

        builder.Metadata.FindNavigation(nameof(Restaurant.Menu))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        // MenuItem is a genuine entity type (not owned), mapped separately in
        // MenuItemConfiguration, so it can use ComplexProperty for Money — a struct,
        // which EF Core's owned-entity APIs (OwnsOne/OwnsMany) don't accept as a
        // dependent type (they require TDependentEntity : class).
        builder.HasMany(r => r.Menu)
            .WithOne()
            .HasForeignKey("RestaurantId")
            .IsRequired();
    }
}

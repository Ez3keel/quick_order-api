using Delivery.Domain.Common;
using Delivery.Domain.Couriers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Delivery.Infrastructure.Persistence.Configurations;

public sealed class CourierConfiguration : IEntityTypeConfiguration<Courier>
{
    public void Configure(EntityTypeBuilder<Courier> builder)
    {
        builder.ToTable("Couriers");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasConversion(id => id.Value, value => CourierId.From(value))
            .ValueGeneratedNever();

        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);

        // GeoCoordinates is a struct and CurrentLocation is *nullable* (a courier with
        // no location yet); packing both components into one "lat|lng" column sidesteps
        // EF Core's optional-complex-type mapping entirely, which is more ceremony than
        // two doubles are worth here.
        builder.Property(c => c.CurrentLocation)
            .HasConversion(
                location => location == null ? null : $"{location.Value.Latitude}|{location.Value.Longitude}",
                value => value == null ? null : ParseLocation(value))
            .HasColumnName("Location")
            .HasMaxLength(64);
    }

    private static GeoCoordinates ParseLocation(string value)
    {
        var parts = value.Split('|');
        return new GeoCoordinates(double.Parse(parts[0]), double.Parse(parts[1]));
    }
}

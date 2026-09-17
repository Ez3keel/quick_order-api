using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Delivery.Infrastructure.Persistence;

public sealed class DeliveryDbContextFactory : IDesignTimeDbContextFactory<DeliveryDbContext>
{
    public DeliveryDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DeliveryDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=quickorder_delivery;Username=postgres;Password=postgres");

        return new DeliveryDbContext(optionsBuilder.Options);
    }
}

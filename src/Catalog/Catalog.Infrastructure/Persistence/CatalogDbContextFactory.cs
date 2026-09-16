using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Catalog.Infrastructure.Persistence;

/// <summary>
/// Lets `dotnet ef migrations add` run against this project directly (design time),
/// without needing to spin up the API host or read its DI container.
/// </summary>
public sealed class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CatalogDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=quickorder_catalog;Username=postgres;Password=postgres");

        return new CatalogDbContext(optionsBuilder.Options);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ordering.Application.Abstractions;
using Ordering.Infrastructure.Catalog;
using Ordering.Infrastructure.Outbox;
using Ordering.Infrastructure.Persistence;

namespace Ordering.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddOrderingInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var postgresConnectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

        services.AddDbContext<OrderingDbContext>(options =>
            options.UseNpgsql(postgresConnectionString));

        var catalogApiBaseUrl = configuration["Services:CatalogApi"]
            ?? throw new InvalidOperationException("Setting 'Services:CatalogApi' is not configured.");

        services.AddHttpClient<ICatalogClient, HttpCatalogClient>(client =>
            client.BaseAddress = new Uri(catalogApiBaseUrl));

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.AddHostedService<OutboxPublisher>();

        return services;
    }
}

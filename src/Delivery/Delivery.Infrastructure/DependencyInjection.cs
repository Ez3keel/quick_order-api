using Delivery.Application.Abstractions;
using Delivery.Infrastructure.Availability;
using Delivery.Infrastructure.Messaging;
using Delivery.Infrastructure.Outbox;
using Delivery.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Delivery.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDeliveryInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var postgresConnectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

        services.AddDbContext<DeliveryDbContext>(options =>
            options.UseNpgsql(postgresConnectionString));

        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Connection string 'Redis' is not configured.");

        services.AddSingleton<IConnectionMultiplexer>(
            _ => ConnectionMultiplexer.Connect(redisConnectionString));

        services.AddScoped<ICourierRepository, CourierRepository>();
        services.AddScoped<IDeliveryAssignmentRepository, DeliveryAssignmentRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<ICourierAvailabilityIndex, RedisCourierAvailabilityIndex>();

        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.Configure<OrderingExchangeOptions>(configuration.GetSection(OrderingExchangeOptions.SectionName));
        services.AddHostedService<OutboxPublisher>();

        return services;
    }

    /// <summary>Separate from AddDeliveryInfrastructure so the consumer only runs in
    /// Delivery.Worker — Delivery.Api manages couriers over HTTP but never assigns.</summary>
    public static IServiceCollection AddDeliveryAssignmentConsumer(this IServiceCollection services)
    {
        services.AddHostedService<OrderReadyForAssignmentConsumer>();
        return services;
    }
}

using Catalog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;

namespace Catalog.IntegrationTests;

/// <summary>
/// Spins up real Postgres, Redis and RabbitMQ containers per test run (not mocks/
/// in-memory fakes) so integration tests exercise the actual EF Core mapping, the
/// actual Redis cache-aside round trip, and the actual Outbox-to-broker publish.
/// </summary>
public sealed class CatalogApiFactory : WebApplicationFactory<Catalog.Api.Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

    private readonly RedisContainer _redis = new RedisBuilder("redis:7-alpine").Build();

    public RabbitMqContainer RabbitMq { get; } = new RabbitMqBuilder("rabbitmq:3.13-management-alpine")
        .WithUsername("guest")
        .WithPassword("guest")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Postgres", _postgres.GetConnectionString());
        builder.UseSetting("ConnectionStrings:Redis", _redis.GetConnectionString());
        builder.UseSetting("RabbitMq:HostName", RabbitMq.Hostname);
        builder.UseSetting("RabbitMq:Port", RabbitMq.GetMappedPublicPort(5672).ToString());
        builder.UseSetting("RabbitMq:UserName", "guest");
        builder.UseSetting("RabbitMq:Password", "guest");
    }

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync(), RabbitMq.StartAsync());

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _redis.DisposeAsync();
        await RabbitMq.DisposeAsync();
        await base.DisposeAsync();
    }
}

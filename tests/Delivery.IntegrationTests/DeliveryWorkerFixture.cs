using System.Diagnostics;
using Delivery.Application;
using Delivery.Infrastructure;
using Delivery.Infrastructure.Observability;
using Delivery.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;

namespace Delivery.IntegrationTests;

/// <summary>
/// Wires up the same services Delivery.Worker's Program.cs does, but as an in-process
/// IHost against real Postgres/Redis/RabbitMQ containers — there is no ASP.NET Core
/// host to reuse here (the worker has no HTTP surface), so this is the equivalent of
/// WebApplicationFactory for a non-web host.
/// </summary>
public sealed class DeliveryWorkerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();
    private readonly RedisContainer _redis = new RedisBuilder("redis:7-alpine").Build();

    public RabbitMqContainer RabbitMq { get; } = new RabbitMqBuilder("rabbitmq:3.13-management-alpine")
        .WithUsername("guest")
        .WithPassword("guest")
        .Build();

    private IHost _host = null!;
    private ActivityListener? _activityListener;

    public IServiceProvider Services => _host.Services;

    public async Task InitializeAsync()
    {
        // ActivitySource.StartActivity returns null unless something is listening —
        // normally that "something" is the OpenTelemetry SDK's TracerProvider
        // (registered in Program.cs), which this in-process test host deliberately
        // doesn't spin up. A minimal listener is enough to make DeliveryActivitySource
        // actually create Activities so the trace-propagation test can observe them.
        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == DeliveryActivitySource.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(_activityListener);

        await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync(), RabbitMq.StartAsync());

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Postgres"] = _postgres.GetConnectionString(),
            ["ConnectionStrings:Redis"] = _redis.GetConnectionString(),
            ["RabbitMq:HostName"] = RabbitMq.Hostname,
            ["RabbitMq:Port"] = RabbitMq.GetMappedPublicPort(5672).ToString(),
            ["RabbitMq:UserName"] = "guest",
            ["RabbitMq:Password"] = "guest",
            ["OrderingExchange:Exchange"] = "ordering.events",
        });

        builder.Services.AddDeliveryApplication();
        builder.Services.AddDeliveryInfrastructure(builder.Configuration);
        builder.Services.AddDeliveryAssignmentConsumer();

        _host = builder.Build();

        using (var scope = _host.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        await _host.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _host.StopAsync();
        _host.Dispose();
        _activityListener?.Dispose();
        await _postgres.DisposeAsync();
        await _redis.DisposeAsync();
        await RabbitMq.DisposeAsync();
    }
}

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;

namespace Notification.IntegrationTests;

public sealed class NotificationApiFactory : WebApplicationFactory<Notification.Api.Program>, IAsyncLifetime
{
    private readonly RedisContainer _redis = new RedisBuilder("redis:7-alpine").Build();

    public RabbitMqContainer RabbitMq { get; } = new RabbitMqBuilder("rabbitmq:3.13-management-alpine")
        .WithUsername("guest")
        .WithPassword("guest")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Redis", _redis.GetConnectionString());
        builder.UseSetting("RabbitMq:HostName", RabbitMq.Hostname);
        builder.UseSetting("RabbitMq:Port", RabbitMq.GetMappedPublicPort(5672).ToString());
        builder.UseSetting("RabbitMq:UserName", "guest");
        builder.UseSetting("RabbitMq:Password", "guest");
        builder.UseSetting("RabbitMq:OrderingExchange", "ordering.events");
        builder.UseSetting("RabbitMq:DeliveryExchange", "delivery.events");
    }

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_redis.StartAsync(), RabbitMq.StartAsync());

        // Touch Services once so ConfigureWebHost's UseSetting calls are applied and
        // the host (with its hosted-service consumers) is actually built and started
        // before any test connects a SignalR client.
        _ = Server;
    }

    public new async Task DisposeAsync()
    {
        await _redis.DisposeAsync();
        await RabbitMq.DisposeAsync();
        await base.DisposeAsync();
    }
}

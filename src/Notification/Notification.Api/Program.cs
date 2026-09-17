using Microsoft.Extensions.DependencyInjection;
using Notification.Api.Hubs;
using Notification.Api.Messaging;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

var redisConnectionString = builder.Configuration.GetConnectionString("Redis")
    ?? throw new InvalidOperationException("Connection string 'Redis' is not configured.");

// The Redis backplane is what makes SignalR work across more than one replica of this
// service: a client connected to instance A gets messages pushed by a consumer running
// in instance B, because both relay through Redis instead of only their own in-memory
// client list.
builder.Services
    .AddSignalR()
    .AddStackExchangeRedis(redisConnectionString, options =>
    {
        options.Configuration.ChannelPrefix = RedisChannel.Literal("quickorder-notification");
    });

builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));
builder.Services.AddSingleton<IEventDeduplicator, RedisEventDeduplicator>();

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.AddHostedService<OrderStatusChangedConsumer>();
builder.Services.AddHostedService<CourierAssignedConsumer>();

var app = builder.Build();

app.MapHub<OrderTrackingHub>("/hubs/order-tracking");

app.Run();

namespace Notification.Api
{
    public partial class Program;
}

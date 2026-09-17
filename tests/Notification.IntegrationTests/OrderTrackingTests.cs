using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using QuickOrder.Contracts.Delivery;
using QuickOrder.Contracts.Messaging;
using QuickOrder.Contracts.Ordering;
using RabbitMQ.Client;

namespace Notification.IntegrationTests;

/// <summary>
/// Drives the real pipeline: publish an integration event to RabbitMQ exactly as
/// Ordering/Delivery would, and assert a real SignalR client (not a mock of IHubContext)
/// actually receives the push over the wire.
/// </summary>
public sealed class OrderTrackingTests(NotificationApiFactory factory) : IClassFixture<NotificationApiFactory>
{
    [Fact]
    public async Task OrderStatusChanged_IsPushedToClientsSubscribedToThatOrder()
    {
        var orderId = Guid.NewGuid();
        await using var connection = await ConnectAsync();

        var received = new TaskCompletionSource<JsonElement>();
        connection.On<JsonElement>("OrderStatusChanged", payload => received.TrySetResult(payload));

        await connection.InvokeAsync("SubscribeToOrder", orderId);
        await PublishAsync("ordering.events", RoutingKey.For<OrderStatusChangedIntegrationEvent>(),
            new OrderStatusChangedIntegrationEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, orderId, "Preparing", "AwaitingCourier"));

        var payload = await WaitAsync(received.Task);
        payload.GetProperty("orderId").GetGuid().Should().Be(orderId);
        payload.GetProperty("newStatus").GetString().Should().Be("AwaitingCourier");
    }

    [Fact]
    public async Task CourierAssigned_IsPushedToBothTheOrderGroupAndTheCourierGroup()
    {
        var orderId = Guid.NewGuid();
        var courierId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        await using var connection = await ConnectAsync();

        var orderNotified = new TaskCompletionSource<JsonElement>();
        var courierNotified = new TaskCompletionSource<JsonElement>();
        connection.On<JsonElement>("CourierAssigned", payload => orderNotified.TrySetResult(payload));
        connection.On<JsonElement>("OrderAssignedToYou", payload => courierNotified.TrySetResult(payload));

        await connection.InvokeAsync("SubscribeToOrder", orderId);
        await connection.InvokeAsync("SubscribeToCourier", courierId);

        await PublishAsync("delivery.events", RoutingKey.For<CourierAssignedIntegrationEvent>(),
            new CourierAssignedIntegrationEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, assignmentId, orderId, courierId));

        var orderPayload = await WaitAsync(orderNotified.Task);
        orderPayload.GetProperty("courierId").GetGuid().Should().Be(courierId);

        var courierPayload = await WaitAsync(courierNotified.Task);
        courierPayload.GetProperty("assignmentId").GetGuid().Should().Be(assignmentId);
    }

    [Fact]
    public async Task RedeliveringTheSameEvent_PushesOnlyOnce()
    {
        var orderId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        await using var connection = await ConnectAsync();

        var receivedCount = 0;
        var firstReceived = new TaskCompletionSource();
        connection.On<JsonElement>("OrderStatusChanged", _ =>
        {
            Interlocked.Increment(ref receivedCount);
            firstReceived.TrySetResult();
        });

        await connection.InvokeAsync("SubscribeToOrder", orderId);

        var integrationEvent = new OrderStatusChangedIntegrationEvent(eventId, DateTimeOffset.UtcNow, orderId, "Received", "Preparing");
        await PublishAsync("ordering.events", RoutingKey.For<OrderStatusChangedIntegrationEvent>(), integrationEvent);
        await WaitAsync(firstReceived.Task);

        await PublishAsync("ordering.events", RoutingKey.For<OrderStatusChangedIntegrationEvent>(), integrationEvent);
        await Task.Delay(TimeSpan.FromSeconds(3));

        receivedCount.Should().Be(1);
    }

    private async Task<HubConnection> ConnectAsync()
    {
        _ = factory.Server; // ensure the host is built

        var connection = new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/order-tracking", options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
            })
            .Build();

        await connection.StartAsync();
        return connection;
    }

    private async Task PublishAsync<TEvent>(string exchange, string routingKey, TEvent integrationEvent)
    {
        var connectionFactory = new ConnectionFactory
        {
            HostName = factory.RabbitMq.Hostname,
            Port = factory.RabbitMq.GetMappedPublicPort(5672),
            UserName = "guest",
            Password = "guest",
        };

        await using var rabbitConnection = await connectionFactory.CreateConnectionAsync();
        await using var channel = await rabbitConnection.CreateChannelAsync();
        await channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(integrationEvent));
        await channel.BasicPublishAsync(exchange, routingKey, body);
    }

    private static async Task<T> WaitAsync<T>(Task<T> task)
    {
        var completed = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(15)));
        if (completed != task)
            throw new TimeoutException("Expected SignalR push did not arrive in time.");

        return await task;
    }

    private static async Task WaitAsync(Task task)
    {
        var completed = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(15)));
        if (completed != task)
            throw new TimeoutException("Expected SignalR push did not arrive in time.");
    }
}

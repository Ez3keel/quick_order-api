using System.Text;
using System.Text.Json;
using Delivery.Application.Couriers.Commands.GoOnline;
using Delivery.Application.Couriers.Commands.RegisterCourier;
using Delivery.Application.Couriers.Dtos;
using Delivery.Infrastructure.Messaging;
using Delivery.Infrastructure.Persistence;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuickOrder.Contracts.Messaging;
using QuickOrder.Contracts.Ordering;
using RabbitMQ.Client;

namespace Delivery.IntegrationTests;

/// <summary>
/// Drives the real consumer end to end: publishes an OrderReadyForAssignmentIntegrationEvent
/// to a RabbitMQ exchange exactly as Ordering's OutboxPublisher would, and checks the
/// consumer's actual side effects (Postgres rows, a courier reserved, a
/// CourierAssignedIntegrationEvent published back out) — not a mock of the handler.
/// </summary>
public sealed class OrderReadyForAssignmentConsumerTests(DeliveryWorkerFixture fixture) : IClassFixture<DeliveryWorkerFixture>
{
    [Fact]
    public async Task PublishingEvent_WithAvailableCourier_AssignsCourierAndPublishesIntegrationEvent()
    {
        var courier = await RegisterAndGoOnlineCourierAsync();
        var orderId = Guid.NewGuid();
        var restaurantId = Guid.NewGuid();

        await using var subscriberConnection = await CreateConnectionAsync();
        await using var subscriberChannel = await subscriberConnection.CreateChannelAsync();
        var deliveryRoutingKey = RoutingKey.For<QuickOrder.Contracts.Delivery.CourierAssignedIntegrationEvent>();
        await subscriberChannel.ExchangeDeclareAsync("delivery.events", ExchangeType.Topic, durable: true);
        var queue = await subscriberChannel.QueueDeclareAsync(exclusive: true);
        await subscriberChannel.QueueBindAsync(queue.QueueName, "delivery.events", deliveryRoutingKey);

        await PublishOrderReadyForAssignmentAsync(Guid.NewGuid(), orderId, restaurantId);

        var payload = await WaitForMessageAsync(subscriberChannel, queue.QueueName, TimeSpan.FromSeconds(20));
        var integrationEvent = JsonSerializer.Deserialize<QuickOrder.Contracts.Delivery.CourierAssignedIntegrationEvent>(payload);
        integrationEvent!.OrderId.Should().Be(orderId);
        integrationEvent.CourierId.Should().Be(courier.Id);

        using var scope = fixture.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>();
        var assignment = await dbContext.Assignments.SingleAsync(a => a.OrderId == orderId);
        assignment.CourierId.Value.Should().Be(courier.Id);

        var updatedCourier = await dbContext.Couriers.SingleAsync(c => c.Id == Delivery.Domain.Couriers.CourierId.From(courier.Id));
        updatedCourier.Status.ToString().Should().Be("Busy");
    }

    [Fact]
    public async Task PublishingEvent_WithTraceParent_PropagatesTheSameTraceIdToTheOutgoingEvent()
    {
        var courier = await RegisterAndGoOnlineCourierAsync();
        var orderId = Guid.NewGuid();
        var restaurantId = Guid.NewGuid();

        await using var subscriberConnection = await CreateConnectionAsync();
        await using var subscriberChannel = await subscriberConnection.CreateChannelAsync();
        var deliveryRoutingKey = RoutingKey.For<QuickOrder.Contracts.Delivery.CourierAssignedIntegrationEvent>();
        await subscriberChannel.ExchangeDeclareAsync("delivery.events", ExchangeType.Topic, durable: true);
        var queue = await subscriberChannel.QueueDeclareAsync(exclusive: true);
        await subscriberChannel.QueueBindAsync(queue.QueueName, "delivery.events", deliveryRoutingKey);

        // A synthetic traceparent, as if it came from Ordering's own OutboxPublisher
        // span. Trace-id is the middle segment: 00-<32 hex trace-id>-<16 hex span-id>-01.
        const string traceId = "4bf92f3577b34da6a3ce929d0e0e4736";
        var incomingTraceParent = $"00-{traceId}-00f067aa0ba902b7-01";

        await PublishOrderReadyForAssignmentAsync(Guid.NewGuid(), orderId, restaurantId, incomingTraceParent);

        var delivery = await WaitForDeliveryAsync(subscriberChannel, queue.QueueName, TimeSpan.FromSeconds(20));
        delivery.BasicProperties.Headers.Should().ContainKey("traceparent");

        var outgoingTraceParent = HeaderValueToString(delivery.BasicProperties.Headers!["traceparent"]);
        outgoingTraceParent.Should().NotBeNull();
        outgoingTraceParent!.Split('-')[1].Should().Be(traceId,
            "the outgoing CourierAssignedIntegrationEvent should stay part of the same trace that started with the incoming OrderReadyForAssignmentIntegrationEvent");
    }

    [Fact]
    public async Task RedeliveringTheSameEvent_DoesNotCreateASecondAssignment()
    {
        var courier = await RegisterAndGoOnlineCourierAsync();
        var orderId = Guid.NewGuid();
        var restaurantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        // Publish twice with the SAME EventId, simulating a RabbitMQ redelivery after
        // a crash right before ack — the exact scenario ProcessedMessages guards against.
        await PublishOrderReadyForAssignmentAsync(eventId, orderId, restaurantId);
        await WaitUntilAssignmentExistsAsync(orderId, TimeSpan.FromSeconds(20));
        await PublishOrderReadyForAssignmentAsync(eventId, orderId, restaurantId);

        await Task.Delay(TimeSpan.FromSeconds(3));

        using var scope = fixture.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>();
        var assignmentCount = await dbContext.Assignments.CountAsync(a => a.OrderId == orderId);
        assignmentCount.Should().Be(1);
    }

    [Fact]
    public async Task PublishingEvent_WithNoCourierAvailable_EndsUpInTheDeadLetterQueueAfterRetries()
    {
        var orderId = Guid.NewGuid();
        var restaurantId = Guid.NewGuid();

        await using var connection = await CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        await channel.QueueDeclareAsync(
            OrderReadyForAssignmentConsumer.DeadLetterQueue, durable: true, exclusive: false, autoDelete: false);

        await PublishOrderReadyForAssignmentAsync(Guid.NewGuid(), orderId, restaurantId);

        // 3 retries at a 5s delay queue TTL each — comfortably under this timeout.
        var payload = await WaitForMessageAsync(
            channel, OrderReadyForAssignmentConsumer.DeadLetterQueue, TimeSpan.FromSeconds(30));

        var integrationEvent = JsonSerializer.Deserialize<OrderReadyForAssignmentIntegrationEvent>(payload);
        integrationEvent!.OrderId.Should().Be(orderId);
    }

    private async Task<CourierDto> RegisterAndGoOnlineCourierAsync()
    {
        using var scope = fixture.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var courier = await sender.Send(new RegisterCourierCommand($"Courier {Guid.NewGuid()}"));
        await sender.Send(new GoOnlineCommand(courier.Id, -23.5505, -46.6333));

        return courier;
    }

    private async Task WaitUntilAssignmentExistsAsync(Guid orderId, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            using var scope = fixture.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>();
            if (await dbContext.Assignments.AnyAsync(a => a.OrderId == orderId))
                return;

            await Task.Delay(TimeSpan.FromMilliseconds(250));
        }

        throw new TimeoutException($"No assignment appeared for order '{orderId}' within {timeout}.");
    }

    private async Task<IConnection> CreateConnectionAsync() => await new ConnectionFactory
    {
        HostName = fixture.RabbitMq.Hostname,
        Port = fixture.RabbitMq.GetMappedPublicPort(5672),
        UserName = "guest",
        Password = "guest",
    }.CreateConnectionAsync();

    private async Task PublishOrderReadyForAssignmentAsync(
        Guid eventId, Guid orderId, Guid restaurantId, string? traceParent = null)
    {
        await using var connection = await CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        await channel.ExchangeDeclareAsync("ordering.events", ExchangeType.Topic, durable: true);

        var integrationEvent = new OrderReadyForAssignmentIntegrationEvent(eventId, DateTimeOffset.UtcNow, orderId, restaurantId);
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(integrationEvent));

        var properties = new BasicProperties();
        if (traceParent is not null)
            properties.Headers = new Dictionary<string, object?> { ["traceparent"] = traceParent };

        await channel.BasicPublishAsync(
            "ordering.events", RoutingKey.For<OrderReadyForAssignmentIntegrationEvent>(), mandatory: false, properties, body);
    }

    private static async Task<string> WaitForMessageAsync(IChannel channel, string queueName, TimeSpan timeout) =>
        Encoding.UTF8.GetString((await WaitForDeliveryAsync(channel, queueName, timeout)).Body.ToArray());

    private static async Task<BasicGetResult> WaitForDeliveryAsync(IChannel channel, string queueName, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            var result = await channel.BasicGetAsync(queueName, autoAck: true);
            if (result is not null)
                return result;

            await Task.Delay(TimeSpan.FromMilliseconds(250));
        }

        throw new TimeoutException($"No message arrived on queue '{queueName}' within {timeout}.");
    }

    private static string? HeaderValueToString(object? value) => value switch
    {
        null => null,
        string s => s,
        byte[] bytes => Encoding.UTF8.GetString(bytes),
        _ => value.ToString(),
    };
}

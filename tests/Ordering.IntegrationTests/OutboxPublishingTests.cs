using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Ordering.Api.Contracts;
using Ordering.Application.Abstractions;
using Ordering.Application.Orders.Dtos;
using QuickOrder.Contracts.Messaging;
using QuickOrder.Contracts.Ordering;
using RabbitMQ.Client;

namespace Ordering.IntegrationTests;

/// <summary>
/// Proves the Outbox pipeline end to end for the event the Delivery Assignment
/// Worker will consume in Fase 4: placing an order and marking it ready for
/// assignment must result in an OrderReadyForAssignmentIntegrationEvent actually
/// arriving on RabbitMQ, not just a domain event object created in memory.
/// </summary>
public sealed class OutboxPublishingTests(OrderingApiFactory factory) : IClassFixture<OrderingApiFactory>
{
    private readonly HttpClient _customerClient = CreateClient(factory, "Customer");
    private readonly HttpClient _restaurantClient = CreateClient(factory, "RestaurantOwner");

    private static HttpClient CreateClient(OrderingApiFactory factory, string role)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtTokenFactory.Create(role, Guid.NewGuid()));
        return client;
    }

    [Fact]
    public async Task MarkingOrderReadyForAssignment_PublishesIntegrationEventToRabbitMq()
    {
        var connectionFactory = new ConnectionFactory
        {
            HostName = factory.RabbitMq.Hostname,
            Port = factory.RabbitMq.GetMappedPublicPort(5672),
            UserName = "guest",
            Password = "guest",
        };

        await using var connection = await connectionFactory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        var routingKey = RoutingKey.For<OrderReadyForAssignmentIntegrationEvent>();
        await channel.ExchangeDeclareAsync("ordering.events", ExchangeType.Topic, durable: true);
        var queue = await channel.QueueDeclareAsync(exclusive: true);
        await channel.QueueBindAsync(queue.QueueName, "ordering.events", routingKey);

        var restaurantId = Guid.NewGuid();
        var menuItemId = Guid.NewGuid();
        factory.CatalogClient.Seed(new CatalogRestaurantSnapshot(
            restaurantId, "Burger House", IsOpen: true,
            [new CatalogMenuItemSnapshot(menuItemId, "X-Burger", 19.90m, "BRL", IsAvailable: true)]));

        var placeResponse = await _customerClient.PostAsJsonAsync(
            "/api/orders", new PlaceOrderRequest(restaurantId, [new PlaceOrderRequestItem(menuItemId, 1)]));
        var order = await placeResponse.Content.ReadFromJsonAsync<OrderDto>();

        await _restaurantClient.PostAsync($"/api/orders/{order!.Id}/start-preparing", null);
        await _restaurantClient.PostAsync($"/api/orders/{order.Id}/mark-ready-for-assignment", null);

        var payload = await WaitForMessageAsync(channel, queue.QueueName, TimeSpan.FromSeconds(15));

        var integrationEvent = JsonSerializer.Deserialize<OrderReadyForAssignmentIntegrationEvent>(payload);
        integrationEvent!.OrderId.Should().Be(order.Id);
        integrationEvent.RestaurantId.Should().Be(restaurantId);
    }

    private static async Task<string> WaitForMessageAsync(IChannel channel, string queueName, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            var result = await channel.BasicGetAsync(queueName, autoAck: true);
            if (result is not null)
                return Encoding.UTF8.GetString(result.Body.ToArray());

            await Task.Delay(TimeSpan.FromMilliseconds(250));
        }

        throw new TimeoutException($"No message arrived on queue '{queueName}' within {timeout}.");
    }
}

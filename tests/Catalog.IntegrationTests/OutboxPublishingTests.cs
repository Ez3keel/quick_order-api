using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Catalog.Api.Contracts;
using Catalog.Application.Restaurants.Dtos;
using FluentAssertions;
using QuickOrder.Contracts.Catalog;
using QuickOrder.Contracts.Messaging;
using RabbitMQ.Client;

namespace Catalog.IntegrationTests;

/// <summary>
/// Proves the Outbox pipeline end to end: an API call that raises a domain event
/// results in a message actually arriving on RabbitMQ — not just a mock verifying a
/// method was called. This is exactly the kind of thing that only an integration test
/// with a real broker can catch (wrong routing key, wrong exchange type, serialization
/// mismatch between producer and the published contract).
/// </summary>
public sealed class OutboxPublishingTests(CatalogApiFactory factory) : IClassFixture<CatalogApiFactory>
{
    private readonly HttpClient _client = CreateAuthenticatedClient(factory);

    private static HttpClient CreateAuthenticatedClient(CatalogApiFactory factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtTokenFactory.Create("RestaurantOwner"));
        return client;
    }

    [Fact]
    public async Task ChangingMenuItemPrice_PublishesIntegrationEventToRabbitMq()
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

        var routingKey = RoutingKey.For<MenuItemPriceChangedIntegrationEvent>();
        await channel.ExchangeDeclareAsync("catalog.events", ExchangeType.Topic, durable: true);
        var queue = await channel.QueueDeclareAsync(exclusive: true);
        await channel.QueueBindAsync(queue.QueueName, "catalog.events", routingKey);

        var registerResponse = await _client.PostAsJsonAsync("/api/restaurants", new RegisterRestaurantRequest("Taco Bell"));
        var restaurant = await registerResponse.Content.ReadFromJsonAsync<RestaurantDto>();

        var itemResponse = await _client.PostAsJsonAsync(
            $"/api/restaurants/{restaurant!.Id}/menu-items", new AddMenuItemRequest("Taco", 10.00m));
        var item = await itemResponse.Content.ReadFromJsonAsync<MenuItemDto>();

        await _client.PutAsJsonAsync(
            $"/api/restaurants/{restaurant.Id}/menu-items/{item!.Id}/price",
            new ChangeMenuItemPriceRequest(12.50m));

        var payload = await WaitForMessageAsync(channel, queue.QueueName, TimeSpan.FromSeconds(15));

        var integrationEvent = JsonSerializer.Deserialize<MenuItemPriceChangedIntegrationEvent>(payload);
        integrationEvent!.RestaurantId.Should().Be(restaurant.Id);
        integrationEvent.MenuItemId.Should().Be(item.Id);
        integrationEvent.PreviousPrice.Should().Be(10.00m);
        integrationEvent.NewPrice.Should().Be(12.50m);
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

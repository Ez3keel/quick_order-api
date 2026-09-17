using System.Net.Http.Headers;
using System.Net.Http.Json;
using Catalog.Api.Contracts;
using Catalog.Application.Restaurants.Dtos;
using FluentAssertions;
using QuickOrder.Contracts.Catalog;
using QuickOrder.Contracts.Messaging;
using RabbitMQ.Client;

namespace Catalog.IntegrationTests;

public sealed class ObservabilityTests(CatalogApiFactory factory) : IClassFixture<CatalogApiFactory>
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
    public async Task MetricsEndpoint_ExposesPrometheusFormattedMetrics()
    {
        var response = await _client.GetAsync("/metrics");

        response.IsSuccessStatusCode.Should().BeTrue();
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("# TYPE").And.Contain("http_server_active_requests");
    }

    [Fact]
    public async Task PublishedIntegrationEvent_CarriesTraceParentHeader_ProvingContextPropagatedThroughOutbox()
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

        var registerResponse = await _client.PostAsJsonAsync("/api/restaurants", new RegisterRestaurantRequest("Trace Diner"));
        var restaurant = await registerResponse.Content.ReadFromJsonAsync<RestaurantDto>();

        var itemResponse = await _client.PostAsJsonAsync(
            $"/api/restaurants/{restaurant!.Id}/menu-items", new AddMenuItemRequest("Burger", 20.00m));
        var item = await itemResponse.Content.ReadFromJsonAsync<MenuItemDto>();

        await _client.PutAsJsonAsync(
            $"/api/restaurants/{restaurant.Id}/menu-items/{item!.Id}/price", new ChangeMenuItemPriceRequest(25.00m));

        var delivery = await WaitForMessageAsync(channel, queue.QueueName, TimeSpan.FromSeconds(15));

        delivery.BasicProperties.Headers.Should().NotBeNull();
        delivery.BasicProperties.Headers.Should().ContainKey("traceparent");
    }

    private static async Task<BasicGetResult> WaitForMessageAsync(IChannel channel, string queueName, TimeSpan timeout)
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
}

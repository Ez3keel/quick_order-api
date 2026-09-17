using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Ordering.Api.Contracts;
using Ordering.Application.Abstractions;
using Ordering.Application.Orders.Dtos;

namespace Ordering.IntegrationTests;

public sealed class OrdersEndpointsTests(OrderingApiFactory factory) : IClassFixture<OrderingApiFactory>
{
    private readonly Guid _customerId = Guid.NewGuid();
    private readonly HttpClient _customerClient = CreateClient(factory, "Customer");
    private readonly HttpClient _restaurantClient = CreateClient(factory, "RestaurantOwner");
    private readonly HttpClient _courierClient = CreateClient(factory, "Courier");

    private static HttpClient CreateClient(OrderingApiFactory factory, string role, Guid? userId = null)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtTokenFactory.Create(role, userId ?? Guid.NewGuid()));
        return client;
    }

    private HttpClient CustomerClient
    {
        get
        {
            _customerClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", TestJwtTokenFactory.Create("Customer", _customerId));
            return _customerClient;
        }
    }

    [Fact]
    public async Task Place_WithAvailableItems_CreatesOrderInReceivedStatus()
    {
        var restaurantId = Guid.NewGuid();
        var menuItemId = Guid.NewGuid();
        factory.CatalogClient.Seed(new CatalogRestaurantSnapshot(
            restaurantId, "Burger House", IsOpen: true,
            [new CatalogMenuItemSnapshot(menuItemId, "X-Burger", 19.90m, "BRL", IsAvailable: true)]));

        var request = new PlaceOrderRequest(restaurantId, [new PlaceOrderRequestItem(menuItemId, 2)]);

        var response = await CustomerClient.PostAsJsonAsync("/api/orders", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        order!.Status.Should().Be("Received");
        order.TotalAmount.Should().Be(39.80m);
        order.CustomerId.Should().Be(_customerId);
        order.Items.Should().ContainSingle(i => i.ProductName == "X-Burger" && i.Quantity == 2);
    }

    [Fact]
    public async Task Place_ForClosedRestaurant_ReturnsConflict()
    {
        var restaurantId = Guid.NewGuid();
        factory.CatalogClient.Seed(new CatalogRestaurantSnapshot(restaurantId, "Closed Place", IsOpen: false, []));

        var request = new PlaceOrderRequest(restaurantId, [new PlaceOrderRequestItem(Guid.NewGuid(), 1)]);

        var response = await CustomerClient.PostAsJsonAsync("/api/orders", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Place_ForUnknownRestaurant_ReturnsConflict()
    {
        var request = new PlaceOrderRequest(Guid.NewGuid(), [new PlaceOrderRequestItem(Guid.NewGuid(), 1)]);

        var response = await CustomerClient.PostAsJsonAsync("/api/orders", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Place_WithoutToken_ReturnsUnauthorized()
    {
        var request = new PlaceOrderRequest(Guid.NewGuid(), [new PlaceOrderRequestItem(Guid.NewGuid(), 1)]);

        var response = await factory.CreateClient().PostAsJsonAsync("/api/orders", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task FullLifecycle_ReceivedToDelivered_TransitionsThroughEveryEndpoint()
    {
        var restaurantId = Guid.NewGuid();
        var menuItemId = Guid.NewGuid();
        factory.CatalogClient.Seed(new CatalogRestaurantSnapshot(
            restaurantId, "Pizza Place", IsOpen: true,
            [new CatalogMenuItemSnapshot(menuItemId, "Margherita", 39.90m, "BRL", IsAvailable: true)]));

        var placeResponse = await CustomerClient.PostAsJsonAsync(
            "/api/orders", new PlaceOrderRequest(restaurantId, [new PlaceOrderRequestItem(menuItemId, 1)]));
        var order = await placeResponse.Content.ReadFromJsonAsync<OrderDto>();

        (await _restaurantClient.PostAsync($"/api/orders/{order!.Id}/start-preparing", null)).StatusCode
            .Should().Be(HttpStatusCode.NoContent);
        (await _restaurantClient.PostAsync($"/api/orders/{order.Id}/mark-ready-for-assignment", null)).StatusCode
            .Should().Be(HttpStatusCode.NoContent);
        (await _courierClient.PostAsync($"/api/orders/{order.Id}/dispatch-for-delivery", null)).StatusCode
            .Should().Be(HttpStatusCode.NoContent);
        (await _courierClient.PostAsync($"/api/orders/{order.Id}/mark-delivered", null)).StatusCode
            .Should().Be(HttpStatusCode.NoContent);

        var finalResponse = await CustomerClient.GetAsync($"/api/orders/{order.Id}");
        var finalOrder = await finalResponse.Content.ReadFromJsonAsync<OrderDto>();
        finalOrder!.Status.Should().Be("Delivered");
    }

    [Fact]
    public async Task GetById_AsADifferentCustomer_ReturnsForbidden()
    {
        var restaurantId = Guid.NewGuid();
        var menuItemId = Guid.NewGuid();
        factory.CatalogClient.Seed(new CatalogRestaurantSnapshot(
            restaurantId, "Ramen House", IsOpen: true,
            [new CatalogMenuItemSnapshot(menuItemId, "Tonkotsu", 45.00m, "BRL", IsAvailable: true)]));

        var placeResponse = await CustomerClient.PostAsJsonAsync(
            "/api/orders", new PlaceOrderRequest(restaurantId, [new PlaceOrderRequestItem(menuItemId, 1)]));
        var order = await placeResponse.Content.ReadFromJsonAsync<OrderDto>();

        var someoneElse = CreateClient(factory, "Customer");
        var response = await someoneElse.GetAsync($"/api/orders/{order!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Cancel_AfterDispatchedForDelivery_ReturnsBadRequest()
    {
        var restaurantId = Guid.NewGuid();
        var menuItemId = Guid.NewGuid();
        factory.CatalogClient.Seed(new CatalogRestaurantSnapshot(
            restaurantId, "Sushi Bar", IsOpen: true,
            [new CatalogMenuItemSnapshot(menuItemId, "Combo", 59.90m, "BRL", IsAvailable: true)]));

        var placeResponse = await CustomerClient.PostAsJsonAsync(
            "/api/orders", new PlaceOrderRequest(restaurantId, [new PlaceOrderRequestItem(menuItemId, 1)]));
        var order = await placeResponse.Content.ReadFromJsonAsync<OrderDto>();

        await _restaurantClient.PostAsync($"/api/orders/{order!.Id}/start-preparing", null);
        await _restaurantClient.PostAsync($"/api/orders/{order.Id}/mark-ready-for-assignment", null);
        await _courierClient.PostAsync($"/api/orders/{order.Id}/dispatch-for-delivery", null);

        var cancelResponse = await CustomerClient.PostAsync($"/api/orders/{order.Id}/cancel", null);

        cancelResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetById_ForUnknownOrder_ReturnsNotFound()
    {
        var response = await CustomerClient.GetAsync($"/api/orders/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

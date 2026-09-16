using System.Net;
using System.Net.Http.Json;
using Catalog.Api.Contracts;
using Catalog.Application.Restaurants.Dtos;
using FluentAssertions;

namespace Catalog.IntegrationTests;

public sealed class RestaurantsEndpointsTests(CatalogApiFactory factory) : IClassFixture<CatalogApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_ThenGetById_ReturnsTheCreatedRestaurant()
    {
        var response = await _client.PostAsJsonAsync("/api/restaurants", new RegisterRestaurantRequest("Burger House"));
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<RestaurantDto>();
        created.Should().NotBeNull();

        var getResponse = await _client.GetAsync($"/api/restaurants/{created!.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var fetched = await getResponse.Content.ReadFromJsonAsync<RestaurantDto>();
        fetched!.Name.Should().Be("Burger House");
        fetched.Menu.Should().BeEmpty();
    }

    [Fact]
    public async Task GetById_ForUnknownRestaurant_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/restaurants/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddMenuItem_ThenGetById_IncludesTheItem()
    {
        var restaurant = await RegisterRestaurant("Pizza Place");

        var itemResponse = await _client.PostAsJsonAsync(
            $"/api/restaurants/{restaurant.Id}/menu-items",
            new AddMenuItemRequest("Margherita", 39.90m));
        itemResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var fetched = await GetRestaurant(restaurant.Id);
        fetched.Menu.Should().ContainSingle(i => i.Name == "Margherita" && i.Price == 39.90m);
    }

    [Fact]
    public async Task AddMenuItem_ForUnknownRestaurant_ReturnsNotFound()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/restaurants/{Guid.NewGuid()}/menu-items",
            new AddMenuItemRequest("Margherita", 39.90m));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ChangeMenuItemPrice_ReflectsOnNextRead_ProvingCacheWasInvalidated()
    {
        var restaurant = await RegisterRestaurant("Sushi Bar");
        var itemResponse = await _client.PostAsJsonAsync(
            $"/api/restaurants/{restaurant.Id}/menu-items",
            new AddMenuItemRequest("Combo 20 peças", 59.90m));
        var item = await itemResponse.Content.ReadFromJsonAsync<MenuItemDto>();

        // Populate the Redis cache-aside entry.
        var beforeChange = await GetRestaurant(restaurant.Id);
        beforeChange.Menu.Single().Price.Should().Be(59.90m);

        var changeResponse = await _client.PutAsJsonAsync(
            $"/api/restaurants/{restaurant.Id}/menu-items/{item!.Id}/price",
            new ChangeMenuItemPriceRequest(69.90m));
        changeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterChange = await GetRestaurant(restaurant.Id);
        afterChange.Menu.Single().Price.Should().Be(69.90m);
    }

    [Fact]
    public async Task AddMenuItem_WithDuplicateName_ReturnsConflict()
    {
        var restaurant = await RegisterRestaurant("Taco Shop");
        await _client.PostAsJsonAsync(
            $"/api/restaurants/{restaurant.Id}/menu-items",
            new AddMenuItemRequest("Taco al Pastor", 15.00m));

        var response = await _client.PostAsJsonAsync(
            $"/api/restaurants/{restaurant.Id}/menu-items",
            new AddMenuItemRequest("Taco al Pastor", 16.00m));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WithBlankName_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/restaurants", new RegisterRestaurantRequest(" "));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<RestaurantDto> RegisterRestaurant(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/restaurants", new RegisterRestaurantRequest(name));
        return (await response.Content.ReadFromJsonAsync<RestaurantDto>())!;
    }

    private async Task<RestaurantDto> GetRestaurant(Guid id)
    {
        var response = await _client.GetAsync($"/api/restaurants/{id}");
        return (await response.Content.ReadFromJsonAsync<RestaurantDto>())!;
    }
}

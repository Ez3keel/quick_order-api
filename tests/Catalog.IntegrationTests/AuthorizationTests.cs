using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Catalog.Api.Contracts;
using FluentAssertions;

namespace Catalog.IntegrationTests;

public sealed class AuthorizationTests(CatalogApiFactory factory) : IClassFixture<CatalogApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task RegisterRestaurant_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/restaurants", new RegisterRestaurantRequest("No Auth"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RegisterRestaurant_WithWrongRole_ReturnsForbidden()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtTokenFactory.Create("Customer"));

        var response = await _client.PostAsJsonAsync("/api/restaurants", new RegisterRestaurantRequest("Wrong Role"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ListRestaurants_WithoutToken_StillWorks()
    {
        // Browsing is deliberately anonymous — only write operations require auth.
        var response = await _client.GetAsync("/api/restaurants");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

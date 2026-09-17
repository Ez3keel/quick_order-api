using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Delivery.Api.Contracts;
using FluentAssertions;

namespace Delivery.IntegrationTests;

public sealed class CouriersAuthorizationTests(DeliveryApiFactory factory) : IClassFixture<DeliveryApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/couriers", new RegisterCourierRequest("No Auth"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_WithWrongRole_ReturnsForbidden()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtTokenFactory.Create("Customer"));

        var response = await _client.PostAsJsonAsync("/api/couriers", new RegisterCourierRequest("Wrong Role"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Register_WithCourierRole_Succeeds()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtTokenFactory.Create("Courier"));

        var response = await _client.PostAsJsonAsync("/api/couriers", new RegisterCourierRequest("João"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}

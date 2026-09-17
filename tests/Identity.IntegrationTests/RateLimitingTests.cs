using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Identity.Api.Contracts;

namespace Identity.IntegrationTests;

/// <summary>
/// Its own factory instance (not shared via IClassFixture with AuthEndpointsTests) so
/// this test's 5-requests-per-minute budget starts fresh and isn't consumed by
/// unrelated tests hitting the same endpoint first.
/// </summary>
public sealed class RateLimitingTests : IAsyncLifetime
{
    private readonly IdentityApiFactory _factory = new() { AuthRateLimitPermitLimit = 5 };
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _factory.InitializeAsync();
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task Login_BeyondTheLimit_ReturnsTooManyRequests()
    {
        var request = new LoginRequest("someone@example.com", "wrong-password");

        for (var i = 0; i < 5; i++)
            await _client.PostAsJsonAsync("/api/auth/login", request);

        var sixthAttempt = await _client.PostAsJsonAsync("/api/auth/login", request);

        sixthAttempt.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}

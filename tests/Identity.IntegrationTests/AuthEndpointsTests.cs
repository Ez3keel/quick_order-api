using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Identity.Api.Contracts;
using Identity.Application.Auth.Dtos;
using Identity.Domain.Users;

namespace Identity.IntegrationTests;

public sealed class AuthEndpointsTests(IdentityApiFactory factory) : IClassFixture<IdentityApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_WithValidData_ReturnsAccessAndRefreshTokens()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest($"{Guid.NewGuid()}@example.com", "correct-horse-battery", UserRole.Customer));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<AuthResultDto>();
        result!.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.Role.Should().Be("Customer");
    }

    [Fact]
    public async Task Register_WithAlreadyRegisteredEmail_ReturnsConflict()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "correct-horse-battery", UserRole.Customer));

        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "another-password1", UserRole.Customer));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_WithCorrectPassword_ReturnsTokens()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "correct-horse-battery", UserRole.Customer));

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "correct-horse-battery"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "correct-horse-battery", UserRole.Customer));

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "wrong-password"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithValidToken_RotatesToANewTokenPair()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        var registerResponse = await _client.PostAsJsonAsync(
            "/api/auth/register", new RegisterRequest(email, "correct-horse-battery", UserRole.Customer));
        var original = await registerResponse.Content.ReadFromJsonAsync<AuthResultDto>();

        var refreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(original!.RefreshToken));

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var rotated = await refreshResponse.Content.ReadFromJsonAsync<AuthResultDto>();
        rotated!.RefreshToken.Should().NotBe(original.RefreshToken);
        rotated.AccessToken.Should().NotBe(original.AccessToken);
    }

    [Fact]
    public async Task Refresh_WithAlreadyRotatedToken_RevokesTheWholeChainAndRejectsFurtherUse()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        var registerResponse = await _client.PostAsJsonAsync(
            "/api/auth/register", new RegisterRequest(email, "correct-horse-battery", UserRole.Customer));
        var original = await registerResponse.Content.ReadFromJsonAsync<AuthResultDto>();

        var firstRefresh = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(original!.RefreshToken));
        var rotated = await firstRefresh.Content.ReadFromJsonAsync<AuthResultDto>();

        // Replaying the already-consumed original token — the theft scenario.
        var replay = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(original.RefreshToken));
        replay.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // The whole chain, including the token issued by the first legitimate
        // refresh, should now be revoked too.
        var secondRefreshAttempt = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(rotated!.RefreshToken));
        secondRefreshAttempt.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Revoke_ThenRefresh_IsRejected()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        var registerResponse = await _client.PostAsJsonAsync(
            "/api/auth/register", new RegisterRequest(email, "correct-horse-battery", UserRole.Customer));
        var original = await registerResponse.Content.ReadFromJsonAsync<AuthResultDto>();

        var revokeResponse = await _client.PostAsJsonAsync("/api/auth/revoke", new RevokeRequest(original!.RefreshToken));
        revokeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var refreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(original.RefreshToken));
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

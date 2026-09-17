using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Ordering.Application.Abstractions;
using Ordering.Application.Orders.Exceptions;
using Ordering.Infrastructure.Catalog;

namespace Ordering.IntegrationTests;

/// <summary>
/// Talks to a real (if deliberately unreliable) HTTP server through the actual
/// resilience pipeline configured in DependencyInjection.cs — not a mocked
/// HttpMessageHandler standing in for Polly's behavior. This is the only way to prove
/// the retry/circuit-breaker wiring is correct: a unit test of the pipeline
/// configuration would prove the options object has the right numbers, not that
/// HttpCatalogClient's calls actually get retried or fail fast when it matters.
/// </summary>
public sealed class CatalogResilienceTests : IAsyncLifetime
{
    private FlakyCatalogServer _server = null!;

    public async Task InitializeAsync() => _server = await FlakyCatalogServer.StartAsync();

    public async Task DisposeAsync() => await _server.DisposeAsync();

    private ICatalogClient BuildClient()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services
            .AddHttpClient<ICatalogClient, HttpCatalogClient>(client => client.BaseAddress = new Uri(_server.BaseAddress))
            .AddCatalogResilience();

        return services.BuildServiceProvider().GetRequiredService<ICatalogClient>();
    }

    [Fact]
    public async Task GetRestaurant_WithTransientFailures_RetriesAndEventuallySucceeds()
    {
        _server.ConfigureFailures(failuresBeforeSucceeding: 2);
        var client = BuildClient();

        var result = await client.GetRestaurantAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Flaky Diner");
        _server.RequestCount.Should().Be(3, "the first 2 calls should have failed and been retried before the 3rd succeeded");
    }

    [Fact]
    public async Task GetRestaurant_WhenCatalogIsPersistentlyDown_ThrowsCatalogUnavailableAfterExhaustingRetries()
    {
        _server.ConfigureFailures(failuresBeforeSucceeding: int.MaxValue);
        var client = BuildClient();

        var act = () => client.GetRestaurantAsync(Guid.NewGuid(), CancellationToken.None);

        await act.Should().ThrowAsync<CatalogUnavailableException>();
        // 1 original attempt + 3 retries configured in CatalogClientResilience.
        _server.RequestCount.Should().Be(4);
    }

    [Fact]
    public async Task GetRestaurant_AfterCircuitOpens_FailsFastWithoutHittingTheServerAgain()
    {
        _server.ConfigureFailures(failuresBeforeSucceeding: int.MaxValue);
        var client = BuildClient();

        // Each call exhausts its own 4 attempts (1 + 3 retries). MinimumThroughput is
        // 4 with a 50% failure ratio, so the breaker should trip within the first call.
        for (var i = 0; i < 3; i++)
        {
            try
            {
                await client.GetRestaurantAsync(Guid.NewGuid(), CancellationToken.None);
            }
            catch (CatalogUnavailableException)
            {
                // expected until the circuit opens
            }
        }

        var requestCountWhileOpen = _server.RequestCount;

        // One more call: if the circuit is open, this should fail immediately without
        // the server's counter moving at all.
        var act = () => client.GetRestaurantAsync(Guid.NewGuid(), CancellationToken.None);
        await act.Should().ThrowAsync<CatalogUnavailableException>();

        _server.RequestCount.Should().Be(requestCountWhileOpen, "an open circuit should fail fast without calling the server again");
    }
}

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ordering.IntegrationTests;

/// <summary>
/// A real Kestrel server standing in for Catalog, deliberately unreliable — this is
/// what makes the resilience tests prove something: HttpCatalogClient talks to this
/// over a real socket, through the real resilience pipeline, not a mocked HttpClient
/// handler that can't exercise timeouts or connection-level failures.
/// </summary>
public sealed class FlakyCatalogServer : IAsyncDisposable
{
    private readonly WebApplication _app;
    private int _requestCount;
    private int _failuresBeforeSucceeding;

    public int RequestCount => _requestCount;
    public string BaseAddress { get; private set; } = null!;

    private FlakyCatalogServer(WebApplication app) => _app = app;

    public static async Task<FlakyCatalogServer> StartAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();

        var app = builder.Build();
        var server = new FlakyCatalogServer(app);

        app.MapGet("/api/restaurants/{id:guid}", (Guid id) =>
        {
            Interlocked.Increment(ref server._requestCount);

            if (server._requestCount <= server._failuresBeforeSucceeding)
                return Results.StatusCode(StatusCodes.Status500InternalServerError);

            return Results.Ok(new
            {
                Id = id,
                Name = "Flaky Diner",
                IsOpen = true,
                Menu = Array.Empty<object>(),
            });
        });

        await app.StartAsync();
        server.BaseAddress = app.Urls.First();
        return server;
    }

    /// <summary>Resets the request counter and configures how many requests fail
    /// (with a 500) before the server starts succeeding. Passing a huge number makes
    /// it always fail, for the circuit-breaker test.</summary>
    public void ConfigureFailures(int failuresBeforeSucceeding)
    {
        _requestCount = 0;
        _failuresBeforeSucceeding = failuresBeforeSucceeding;
    }

    public async ValueTask DisposeAsync() => await _app.DisposeAsync();
}

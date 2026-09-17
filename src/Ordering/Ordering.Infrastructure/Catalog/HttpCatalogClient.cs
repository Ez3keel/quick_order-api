using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ordering.Application.Abstractions;
using Ordering.Application.Orders.Exceptions;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Ordering.Infrastructure.Catalog;

/// <summary>
/// Synchronous HTTP call to Catalog, used only when placing an order — the one place
/// Ordering needs a live price/availability check, and the only true synchronous
/// cross-service HTTP call anywhere in this system (everything else goes through
/// RabbitMQ). The retry/circuit-breaker/timeout pipeline itself is registered in
/// DependencyInjection.cs (Microsoft.Extensions.Http.Resilience wraps this HttpClient
/// transparently); this class only needs to translate the pipeline's failure
/// exceptions into something Application can reason about without knowing Polly exists.
/// </summary>
public sealed class HttpCatalogClient(HttpClient httpClient) : ICatalogClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<CatalogRestaurantSnapshot?> GetRestaurantAsync(Guid restaurantId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await httpClient.GetAsync($"/api/restaurants/{restaurantId}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<CatalogRestaurantSnapshot>(JsonOptions, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or BrokenCircuitException or TimeoutRejectedException)
        {
            // Every retry the pipeline was going to attempt already happened before
            // this exception reached us — by the time we're here, Catalog is either
            // genuinely down or the circuit is deliberately open to give it room to
            // recover. Either way, Application shouldn't see a raw HttpRequestException.
            throw new CatalogUnavailableException(exception);
        }
    }
}

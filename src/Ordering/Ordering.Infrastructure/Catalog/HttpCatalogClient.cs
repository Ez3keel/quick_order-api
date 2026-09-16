using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ordering.Application.Abstractions;

namespace Ordering.Infrastructure.Catalog;

/// <summary>
/// Synchronous HTTP call to Catalog, used only when placing an order — the one place
/// Ordering needs a live price/availability check. No retry policy yet; that's Fase 7
/// (Polly), once resilience becomes a named goal instead of an accident of timing.
/// </summary>
public sealed class HttpCatalogClient(HttpClient httpClient) : ICatalogClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<CatalogRestaurantSnapshot?> GetRestaurantAsync(Guid restaurantId, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync($"/api/restaurants/{restaurantId}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<CatalogRestaurantSnapshot>(JsonOptions, cancellationToken);
    }
}

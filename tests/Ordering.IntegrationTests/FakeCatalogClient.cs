using System.Collections.Concurrent;
using Ordering.Application.Abstractions;

namespace Ordering.IntegrationTests;

/// <summary>
/// Stands in for the real Catalog HTTP call in these tests. Ordering's integration
/// tests exercise Ordering's own infrastructure (its real Postgres, its real EF Core
/// mapping) — Catalog is a separate service with its own integration suite, so faking
/// the boundary here keeps this suite fast and independent of Catalog actually running.
/// </summary>
public sealed class FakeCatalogClient : ICatalogClient
{
    private readonly ConcurrentDictionary<Guid, CatalogRestaurantSnapshot> _restaurants = new();

    public void Seed(CatalogRestaurantSnapshot restaurant) => _restaurants[restaurant.Id] = restaurant;

    public Task<CatalogRestaurantSnapshot?> GetRestaurantAsync(Guid restaurantId, CancellationToken cancellationToken) =>
        Task.FromResult(_restaurants.GetValueOrDefault(restaurantId));
}

using Catalog.Application.Restaurants.Dtos;

namespace Catalog.Application.Abstractions;

/// <summary>
/// Cache-aside for menu reads: callers check the cache first, fall back to the
/// repository on a miss, then populate the cache. Writes invalidate instead of
/// updating the cache directly, keeping the cache a pure derived copy of Postgres.
/// </summary>
public interface IMenuCache
{
    Task<RestaurantDto?> GetAsync(Guid restaurantId, CancellationToken cancellationToken);

    Task SetAsync(RestaurantDto restaurant, CancellationToken cancellationToken);

    Task InvalidateAsync(Guid restaurantId, CancellationToken cancellationToken);
}

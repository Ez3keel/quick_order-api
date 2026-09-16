using System.Text.Json;
using Catalog.Application.Abstractions;
using Catalog.Application.Restaurants.Dtos;
using StackExchange.Redis;

namespace Catalog.Infrastructure.Caching;

public sealed class RedisMenuCache(IConnectionMultiplexer connectionMultiplexer) : IMenuCache
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);

    private IDatabase Database => connectionMultiplexer.GetDatabase();

    public async Task<RestaurantDto?> GetAsync(Guid restaurantId, CancellationToken cancellationToken)
    {
        var value = await Database.StringGetAsync(KeyFor(restaurantId));
        return value.IsNullOrEmpty ? null : JsonSerializer.Deserialize<RestaurantDto>((string)value!);
    }

    public Task SetAsync(RestaurantDto restaurant, CancellationToken cancellationToken) =>
        Database.StringSetAsync(KeyFor(restaurant.Id), JsonSerializer.Serialize(restaurant), Ttl);

    public Task InvalidateAsync(Guid restaurantId, CancellationToken cancellationToken) =>
        Database.KeyDeleteAsync(KeyFor(restaurantId));

    private static string KeyFor(Guid restaurantId) => $"catalog:restaurant:{restaurantId}";
}

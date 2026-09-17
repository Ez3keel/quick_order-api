using Delivery.Application.Abstractions;
using StackExchange.Redis;

namespace Delivery.Infrastructure.Availability;

/// <summary>
/// Couriers are stored in a Redis GEO set (a sorted set under the hood) keyed by
/// courier id, so the same structure that answers "who's free" today is ready for a
/// "who's free near this restaurant" query later — GEOADD/GEOSEARCH need no schema
/// change, just a different read.
/// </summary>
public sealed class RedisCourierAvailabilityIndex(IConnectionMultiplexer connectionMultiplexer) : ICourierAvailabilityIndex
{
    private const string Key = "delivery:couriers:available";

    private IDatabase Database => connectionMultiplexer.GetDatabase();

    public Task MarkAvailableAsync(Guid courierId, double latitude, double longitude, CancellationToken cancellationToken) =>
        Database.GeoAddAsync(Key, longitude, latitude, courierId.ToString());

    public Task MarkUnavailableAsync(Guid courierId, CancellationToken cancellationToken) =>
        Database.SortedSetRemoveAsync(Key, courierId.ToString());

    public async Task<Guid?> PickAvailableCourierAsync(CancellationToken cancellationToken)
    {
        var member = await Database.SortedSetRandomMemberAsync(Key);
        return member.IsNullOrEmpty ? null : Guid.Parse((string)member!);
    }
}

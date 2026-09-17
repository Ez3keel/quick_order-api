using StackExchange.Redis;

namespace Notification.Api.Messaging;

public sealed class RedisEventDeduplicator(IConnectionMultiplexer connectionMultiplexer) : IEventDeduplicator
{
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(1);

    public Task<bool> TryMarkProcessedAsync(Guid eventId, CancellationToken cancellationToken) =>
        connectionMultiplexer.GetDatabase().StringSetAsync(
            $"notification:processed:{eventId}", value: "1", expiry: Ttl, when: When.NotExists);
}

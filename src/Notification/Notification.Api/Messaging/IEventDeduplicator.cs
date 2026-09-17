namespace Notification.Api.Messaging;

/// <summary>
/// Notification has no Postgres of its own — it's a pure relay with no aggregates to
/// own, so a ProcessedMessages table (the pattern used in Delivery, Fase 4) would be
/// infrastructure with nothing else to justify it. Redis, which this service already
/// needs as the SignalR backplane, doubles as the dedup store: a SETNX with a TTL is
/// enough to swallow a RabbitMQ redelivery within the window that matters (a customer
/// seeing the same "order is out for delivery" push twice is a UX blemish, not a
/// correctness bug — unlike Delivery, where a duplicate would double-book a courier).
/// </summary>
public interface IEventDeduplicator
{
    Task<bool> TryMarkProcessedAsync(Guid eventId, CancellationToken cancellationToken);
}

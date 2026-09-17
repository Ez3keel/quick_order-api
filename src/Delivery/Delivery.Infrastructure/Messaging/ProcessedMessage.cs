namespace Delivery.Infrastructure.Messaging;

/// <summary>
/// Dedup record for the consumer side of the Outbox/queue pipeline. RabbitMQ delivers
/// at-least-once (a redelivery after a crash before ack, or a broker-side retry, is
/// normal), so the consumer checks this table before acting and inserts a row after —
/// same idea as the outbox, mirrored on the receiving end.
/// </summary>
public sealed class ProcessedMessage
{
    public Guid EventId { get; private set; }
    public string EventType { get; private set; } = null!;
    public DateTimeOffset ProcessedOnUtc { get; private set; }

    private ProcessedMessage() { }

    public static ProcessedMessage Create(Guid eventId, string eventType, DateTimeOffset processedOnUtc) => new()
    {
        EventId = eventId,
        EventType = eventType,
        ProcessedOnUtc = processedOnUtc,
    };
}

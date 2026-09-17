namespace Ordering.Infrastructure.Outbox;

public sealed class OutboxMessage
{
    public Guid Id { get; private set; }
    public string Type { get; private set; } = null!;
    public string Content { get; private set; } = null!;
    public DateTimeOffset OccurredOnUtc { get; private set; }
    public DateTimeOffset? ProcessedOnUtc { get; private set; }
    public string? Error { get; private set; }
    public int RetryCount { get; private set; }
    public DateTimeOffset? NextAttemptAtUtc { get; private set; }

    /// <summary>The W3C traceparent of whatever caused this event, captured at
    /// creation time so OutboxPublisher can resume that trace even though the actual
    /// RabbitMQ publish happens later, disconnected from any HTTP request pipeline —
    /// see QuickOrder.Contracts.Messaging.TraceContextPropagation.</summary>
    public string? TraceParent { get; private set; }

    public const int MaxRetries = 5;

    private OutboxMessage() { }

    public static OutboxMessage Create(
        Guid id, string type, string content, DateTimeOffset occurredOnUtc, string? traceParent = null) => new()
    {
        Id = id,
        Type = type,
        Content = content,
        OccurredOnUtc = occurredOnUtc,
        TraceParent = traceParent,
    };

    public void MarkProcessed(DateTimeOffset processedOnUtc)
    {
        ProcessedOnUtc = processedOnUtc;
        Error = null;
    }

    public bool CanRetry => RetryCount < MaxRetries;

    /// <summary>Exponential backoff (2s, 4s, 8s, 16s, 32s) so a broker outage doesn't
    /// turn the publisher's poll loop into a tight retry storm against Postgres/RabbitMQ.</summary>
    public void MarkFailed(string error, DateTimeOffset now)
    {
        Error = error;
        RetryCount++;
        NextAttemptAtUtc = now.AddSeconds(Math.Pow(2, RetryCount));
    }
}

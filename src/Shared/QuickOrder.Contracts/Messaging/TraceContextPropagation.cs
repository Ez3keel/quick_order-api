using System.Diagnostics;

namespace QuickOrder.Contracts.Messaging;

/// <summary>
/// HTTP gets distributed tracing "for free" from OpenTelemetry's ASP.NET Core/HttpClient
/// instrumentation, which knows to read and write the W3C traceparent header. RabbitMQ
/// has no such built-in instrumentation — a message is just bytes and a header
/// dictionary, so whoever publishes has to manually stash the current trace context in
/// there, and whoever consumes has to manually read it back out and link the new
/// "process" span to it. Without this, every trace would restart from zero at each
/// consumer instead of showing the full HTTP request -> queue -> consumer -> queue ->
/// consumer chain as one connected trace.
///
/// Shared here (not duplicated per service) for the same reason as RoutingKey: this is
/// wire format, not domain logic — producer and consumer must agree on the exact
/// header names and encoding.
/// </summary>
public static class TraceContextPropagation
{
    private const string TraceParentHeader = "traceparent";
    private const string TraceStateHeader = "tracestate";

    /// <summary>Call right before publishing, with the Activity that represents the
    /// publish operation (or Activity.Current if publishing isn't itself traced).</summary>
    public static void Inject(Activity? activity, IDictionary<string, object?> headers)
    {
        if (activity is null)
            return;

        headers[TraceParentHeader] = activity.Id;

        if (!string.IsNullOrEmpty(activity.TraceStateString))
            headers[TraceStateHeader] = activity.TraceStateString;
    }

    /// <summary>Call when a message is received, before starting the consumer's own
    /// Activity, so that Activity can be linked as a child of the producer's span.</summary>
    public static ActivityContext Extract(IDictionary<string, object?>? headers)
    {
        if (headers is null || !headers.TryGetValue(TraceParentHeader, out var value) || value is null)
            return default;

        var traceParent = HeaderValueToString(value);
        if (traceParent is null || !ActivityContext.TryParse(traceParent, ExtractTraceState(headers), out var context))
            return default;

        return context;
    }

    private static string? ExtractTraceState(IDictionary<string, object?> headers) =>
        headers.TryGetValue(TraceStateHeader, out var value) ? HeaderValueToString(value) : null;

    /// <summary>RabbitMQ.Client hands back header values as byte[] over the wire, but
    /// as the original object when read back within the same process (e.g. our own
    /// integration tests) — this accepts either.</summary>
    private static string? HeaderValueToString(object? value) => value switch
    {
        null => null,
        string s => s,
        byte[] bytes => System.Text.Encoding.UTF8.GetString(bytes),
        _ => value.ToString(),
    };
}

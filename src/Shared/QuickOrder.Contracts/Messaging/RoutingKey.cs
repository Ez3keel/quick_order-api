namespace QuickOrder.Contracts.Messaging;

/// <summary>
/// The routing key for an integration event is just its CLR type name. Both the
/// Outbox publisher (producer side) and every consumer derive it the same way, so
/// there's one source of truth for "what string goes on the wire for this event".
/// </summary>
public static class RoutingKey
{
    public static string For<TEvent>() where TEvent : IIntegrationEvent => typeof(TEvent).Name;

    public static string For(Type eventType) => eventType.Name;
}

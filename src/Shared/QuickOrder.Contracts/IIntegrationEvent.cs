namespace QuickOrder.Contracts;

/// <summary>
/// Marker for the message shapes published to RabbitMQ. This assembly is the one
/// deliberate exception to "no shared kernel between services" (see
/// docs/DECISOES-DE-ARQUITETURA.md item 2): consumers need the exact wire shape a
/// producer publishes, so the contract itself — not the domain model behind it — is
/// shared and versioned like any other published API.
/// </summary>
public interface IIntegrationEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredOnUtc { get; }
}

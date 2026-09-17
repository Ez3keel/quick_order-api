namespace Delivery.Application.Assignments.Exceptions;

/// <summary>Raised when no courier is online and free right now. Treated as a
/// transient failure by the consumer (Fase 4) — retried with backoff, same as a
/// broker hiccup, since a courier may come online any moment.</summary>
public sealed class NoCourierAvailableException : Exception
{
    public Guid OrderId { get; }

    public NoCourierAvailableException(Guid orderId)
        : base($"No courier is available to take order '{orderId}' right now.")
    {
        OrderId = orderId;
    }
}

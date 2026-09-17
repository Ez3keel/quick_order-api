namespace Delivery.Domain.Common;

/// <summary>Lets Infrastructure collect and drain domain events off any aggregate
/// without depending on the concrete AggregateRoot&lt;TId&gt; generic type.</summary>
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}

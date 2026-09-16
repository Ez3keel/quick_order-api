namespace Delivery.Domain.Common;

public interface IDomainEvent
{
    DateTimeOffset OccurredOn { get; }
}

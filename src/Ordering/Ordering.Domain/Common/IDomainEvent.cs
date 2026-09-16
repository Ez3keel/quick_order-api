namespace Ordering.Domain.Common;

public interface IDomainEvent
{
    DateTimeOffset OccurredOn { get; }
}

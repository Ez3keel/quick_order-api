namespace Catalog.Domain.Common;

public interface IDomainEvent
{
    DateTimeOffset OccurredOn { get; }
}

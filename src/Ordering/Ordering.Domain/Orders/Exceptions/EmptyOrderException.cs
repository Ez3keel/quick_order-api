namespace Ordering.Domain.Orders.Exceptions;

public sealed class EmptyOrderException : Exception
{
    public EmptyOrderException() : base("An order must contain at least one item.") { }
}

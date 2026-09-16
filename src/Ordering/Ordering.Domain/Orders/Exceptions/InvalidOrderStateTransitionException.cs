namespace Ordering.Domain.Orders.Exceptions;

public sealed class InvalidOrderStateTransitionException : Exception
{
    public OrderStatus From { get; }
    public OrderStatus To { get; }

    public InvalidOrderStateTransitionException(OrderStatus from, OrderStatus to)
        : base($"Cannot transition order from '{from}' to '{to}'.")
    {
        From = from;
        To = to;
    }
}

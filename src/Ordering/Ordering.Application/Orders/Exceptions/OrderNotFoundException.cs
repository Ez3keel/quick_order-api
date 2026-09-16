namespace Ordering.Application.Orders.Exceptions;

public sealed class OrderNotFoundException : Exception
{
    public Guid OrderId { get; }

    public OrderNotFoundException(Guid orderId) : base($"Order '{orderId}' was not found.")
    {
        OrderId = orderId;
    }
}

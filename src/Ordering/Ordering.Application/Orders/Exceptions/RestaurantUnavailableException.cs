namespace Ordering.Application.Orders.Exceptions;

/// <summary>Raised when placing an order against a restaurant Catalog doesn't know
/// about, or one that's currently closed.</summary>
public sealed class RestaurantUnavailableException : Exception
{
    public Guid RestaurantId { get; }

    public RestaurantUnavailableException(Guid restaurantId)
        : base($"Restaurant '{restaurantId}' is not available to take orders right now.")
    {
        RestaurantId = restaurantId;
    }
}

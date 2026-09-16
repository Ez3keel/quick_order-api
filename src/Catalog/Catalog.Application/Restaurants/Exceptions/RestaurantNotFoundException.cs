namespace Catalog.Application.Restaurants.Exceptions;

/// <summary>
/// Application-level "not found" — unlike the domain exceptions in Catalog.Domain,
/// this isn't an invariant violation, just a lookup miss. Mapped to HTTP 404 by the
/// API's exception-handling middleware.
/// </summary>
public sealed class RestaurantNotFoundException : Exception
{
    public Guid RestaurantId { get; }

    public RestaurantNotFoundException(Guid restaurantId)
        : base($"Restaurant '{restaurantId}' was not found.")
    {
        RestaurantId = restaurantId;
    }
}

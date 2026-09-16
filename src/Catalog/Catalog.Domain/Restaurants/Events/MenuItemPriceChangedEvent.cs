using Catalog.Domain.Common;

namespace Catalog.Domain.Restaurants.Events;

/// <summary>
/// Raised whenever a menu item's price changes. The API layer uses this to invalidate
/// the Redis cache-aside entry for this restaurant's menu instead of waiting for it to expire.
/// </summary>
public sealed record MenuItemPriceChangedEvent(
    RestaurantId RestaurantId,
    MenuItemId MenuItemId,
    Money PreviousPrice,
    Money NewPrice,
    DateTimeOffset OccurredOn) : IDomainEvent;

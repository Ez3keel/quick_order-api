namespace QuickOrder.Contracts.Catalog;

public sealed record MenuItemPriceChangedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid RestaurantId,
    Guid MenuItemId,
    decimal PreviousPrice,
    string PreviousCurrency,
    decimal NewPrice,
    string NewCurrency) : IIntegrationEvent;

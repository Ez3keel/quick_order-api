namespace Catalog.Api.Contracts;

public sealed record RegisterRestaurantRequest(string Name);

public sealed record AddMenuItemRequest(string ProductName, decimal Price, string Currency = "BRL");

public sealed record ChangeMenuItemPriceRequest(decimal NewPrice, string Currency = "BRL");

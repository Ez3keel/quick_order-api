namespace Ordering.Application.Abstractions;

/// <summary>
/// Ordering's read-only view of the Catalog service, consumed synchronously over HTTP
/// when placing an order (the one place Ordering needs a live price/availability
/// check). Everything else about Catalog reaches Ordering asynchronously via events,
/// once the Outbox/RabbitMQ pipeline exists (Fase 3+).
/// </summary>
public interface ICatalogClient
{
    Task<CatalogRestaurantSnapshot?> GetRestaurantAsync(Guid restaurantId, CancellationToken cancellationToken);
}

public sealed record CatalogRestaurantSnapshot(
    Guid Id,
    string Name,
    bool IsOpen,
    IReadOnlyCollection<CatalogMenuItemSnapshot> Menu);

public sealed record CatalogMenuItemSnapshot(
    Guid Id,
    string Name,
    decimal Price,
    string Currency,
    bool IsAvailable);

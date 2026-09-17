namespace Delivery.Application.Abstractions;

/// <summary>
/// Redis-backed live index of "who's online and free right now" — separate from
/// Postgres (the source of truth for a courier's identity and history) because this
/// data changes constantly and querying it needs to be cheap and immediate, not a
/// database round trip. Couriers are stored as GEO members so the index is ready for
/// a future nearest-courier-to-restaurant query once Catalog models restaurant
/// coordinates; today's pick is a random available member.
/// </summary>
public interface ICourierAvailabilityIndex
{
    Task MarkAvailableAsync(Guid courierId, double latitude, double longitude, CancellationToken cancellationToken);

    Task MarkUnavailableAsync(Guid courierId, CancellationToken cancellationToken);

    Task<Guid?> PickAvailableCourierAsync(CancellationToken cancellationToken);
}

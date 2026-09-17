namespace Ordering.Application.Orders.Exceptions;

/// <summary>
/// Distinct from RestaurantUnavailableException (409 — the restaurant itself is
/// closed/unknown, a business fact Catalog told us about) — this means the call to
/// Catalog never got a real answer at all: timed out, kept failing through every
/// retry, or the circuit breaker is open. Mapped to 503, not 409: the client should
/// retry shortly, not treat this as "that restaurant can't take orders".
/// </summary>
public sealed class CatalogUnavailableException : Exception
{
    public CatalogUnavailableException(Exception innerException)
        : base("The Catalog service could not be reached.", innerException) { }
}

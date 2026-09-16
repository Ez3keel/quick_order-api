using System.Text.Json;
using Catalog.Domain.Common;
using Catalog.Domain.Restaurants.Events;
using QuickOrder.Contracts.Catalog;
using QuickOrder.Contracts.Messaging;

namespace Catalog.Infrastructure.Outbox;

/// <summary>Translates a domain event (internal to Catalog.Domain) into the
/// integration event contract published on the wire. Kept separate from the domain
/// event itself on purpose: what other services see is a public API and shouldn't
/// change every time an internal domain event's shape changes.</summary>
public static class CatalogIntegrationEventMapper
{
    public static OutboxMessage? TryMapToOutboxMessage(IDomainEvent domainEvent)
    {
        object? integrationEvent = domainEvent switch
        {
            MenuItemPriceChangedEvent e => new MenuItemPriceChangedIntegrationEvent(
                Guid.NewGuid(),
                e.OccurredOn,
                e.RestaurantId.Value,
                e.MenuItemId.Value,
                e.PreviousPrice.Amount,
                e.PreviousPrice.Currency,
                e.NewPrice.Amount,
                e.NewPrice.Currency),
            _ => null,
        };

        if (integrationEvent is null)
            return null;

        var routingKey = RoutingKey.For(integrationEvent.GetType());
        var content = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType());

        return OutboxMessage.Create(Guid.NewGuid(), routingKey, content, domainEvent.OccurredOn);
    }
}

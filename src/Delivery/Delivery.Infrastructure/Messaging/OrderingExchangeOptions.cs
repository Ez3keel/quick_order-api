namespace Delivery.Infrastructure.Messaging;

/// <summary>Which exchange Delivery listens to for Ordering's events — a cross-service
/// setting (the name Ordering's own OutboxPublisher declares), not Delivery's own
/// broker connection, so it's configured separately from RabbitMqOptions.</summary>
public sealed class OrderingExchangeOptions
{
    public const string SectionName = "OrderingExchange";

    public string Exchange { get; set; } = "ordering.events";
}

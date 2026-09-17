namespace Notification.Api.Messaging;

/// <summary>
/// Notification never publishes — it only listens to exchanges Ordering and Delivery
/// already declare — so unlike the other services' RabbitMqOptions, there's no
/// "own exchange" here, just the two upstream exchange names it binds queues to.
/// </summary>
public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string OrderingExchange { get; set; } = "ordering.events";
    public string DeliveryExchange { get; set; } = "delivery.events";
}

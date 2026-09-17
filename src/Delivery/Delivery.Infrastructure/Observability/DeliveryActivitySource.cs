using System.Diagnostics;

namespace Delivery.Infrastructure.Observability;

public static class DeliveryActivitySource
{
    public const string Name = "QuickOrder.Delivery";

    public static readonly ActivitySource Instance = new(Name);
}

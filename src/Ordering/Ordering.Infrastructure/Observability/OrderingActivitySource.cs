using System.Diagnostics;

namespace Ordering.Infrastructure.Observability;

public static class OrderingActivitySource
{
    public const string Name = "QuickOrder.Ordering";

    public static readonly ActivitySource Instance = new(Name);
}

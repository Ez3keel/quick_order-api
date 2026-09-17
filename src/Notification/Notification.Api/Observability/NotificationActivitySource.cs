using System.Diagnostics;

namespace Notification.Api.Observability;

public static class NotificationActivitySource
{
    public const string Name = "QuickOrder.Notification";

    public static readonly ActivitySource Instance = new(Name);
}

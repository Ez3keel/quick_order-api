using Microsoft.AspNetCore.SignalR;

namespace Notification.Api.Hubs;

/// <summary>
/// Clients (customer app, courier app) join a group for whatever they care about —
/// there's no server-side notion of "who is allowed to see this order" here, that
/// authorization belongs to Ordering/Delivery's own APIs (Fase 6). Groups are named
/// so any consumer publishing to a group name derived the same way reaches the
/// right clients without a lookup.
/// </summary>
public sealed class OrderTrackingHub : Hub
{
    public Task SubscribeToOrder(Guid orderId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, GroupNames.ForOrder(orderId));

    public Task UnsubscribeFromOrder(Guid orderId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupNames.ForOrder(orderId));

    public Task SubscribeToCourier(Guid courierId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, GroupNames.ForCourier(courierId));

    public Task UnsubscribeFromCourier(Guid courierId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupNames.ForCourier(courierId));
}

public static class GroupNames
{
    public static string ForOrder(Guid orderId) => $"order-{orderId}";

    public static string ForCourier(Guid courierId) => $"courier-{courierId}";
}

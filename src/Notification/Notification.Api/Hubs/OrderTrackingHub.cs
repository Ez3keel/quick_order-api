using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Notification.Api.Hubs;

/// <summary>
/// Now requires a valid JWT to connect at all ([Authorize] — see Program.cs for how
/// the token reaches SignalR, since WebSocket connections can't send a normal
/// Authorization header). SubscribeToCourier enforces that a courier can only join
/// their own group; SubscribeToOrder still doesn't check that the caller owns that
/// order — that would need a cross-service lookup this Hub doesn't have, the same
/// class of gap noted in docs item 32. Authentication (who you are) is now enforced;
/// full authorization (which orders you may see) is still a follow-up.
/// </summary>
[Authorize]
public sealed class OrderTrackingHub : Hub
{
    public Task SubscribeToOrder(Guid orderId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, GroupNames.ForOrder(orderId));

    public Task UnsubscribeFromOrder(Guid orderId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupNames.ForOrder(orderId));

    public Task SubscribeToCourier(Guid courierId)
    {
        var callerId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (callerId != courierId.ToString() && Context.User?.IsInRole("Admin") != true)
            throw new HubException("You can only subscribe to your own courier group.");

        return Groups.AddToGroupAsync(Context.ConnectionId, GroupNames.ForCourier(courierId));
    }

    public Task UnsubscribeFromCourier(Guid courierId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupNames.ForCourier(courierId));
}

public static class GroupNames
{
    public static string ForOrder(Guid orderId) => $"order-{orderId}";

    public static string ForCourier(Guid courierId) => $"courier-{courierId}";
}

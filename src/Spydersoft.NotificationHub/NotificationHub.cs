using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Spydersoft.NotificationHub;

/// <summary>
/// Push-only hub — clients never invoke hub methods, they just listen for "ReceiveNotification".
/// One SignalR group per user (<c>user:{userId}</c>) so all of a user's open tabs/devices get
/// the push. See plans/notifications/realtime-spec.md.
/// </summary>
[Authorize]
public sealed class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(userId));
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // SignalR removes the connection from all groups automatically on disconnect.
        await base.OnDisconnectedAsync(exception);
    }

    internal static string GroupName(string userId) => $"user:{userId}";

    private string GetUserId() =>
        Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? Context.User?.FindFirstValue("sub")
        ?? throw new InvalidOperationException("Authenticated connection is missing a user id claim.");
}

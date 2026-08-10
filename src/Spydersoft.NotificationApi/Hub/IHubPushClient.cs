using Spydersoft.Notification.Contracts;

namespace Spydersoft.NotificationApi.Hub;

public interface IHubPushClient
{
    /// <summary>
    /// Fire-and-forget push to the hub's internal endpoint. Returns false (never throws) on any
    /// failure — the caller records the InApp delivery as Failed/Skipped and moves on; the client
    /// still gets the notification on next poll. See plans/notifications/realtime-spec.md.
    /// </summary>
    Task<bool> PushAsync(string userId, NotificationPushDto notification, CancellationToken cancellationToken = default);
}

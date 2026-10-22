using Spydersoft.Notification.Contracts;

namespace Spydersoft.NotificationApi.Channels;

public interface INotificationChannelSender
{
    NotificationChannel Channel { get; }

    Task<ChannelSendResult> SendAsync(NotificationPushDto notification, string destination, CancellationToken cancellationToken = default);
}

public sealed record ChannelSendResult(bool Success, string? ExternalId, string? Error);

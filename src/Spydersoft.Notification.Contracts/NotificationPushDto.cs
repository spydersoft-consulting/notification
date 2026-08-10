namespace Spydersoft.Notification.Contracts;

/// <summary>Payload pushed by the hub's "ReceiveNotification" SignalR event.</summary>
public sealed record NotificationPushDto(
    Guid Id,
    string Source,
    string Type,
    string Subject,
    string Body,
    NotificationPriority Priority,
    DateTimeOffset CreatedAt);

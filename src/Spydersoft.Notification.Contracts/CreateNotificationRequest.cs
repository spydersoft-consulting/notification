namespace Spydersoft.Notification.Contracts;

public sealed record CreateNotificationRequest(
    string UserId,
    string Source,
    string Type,
    string Subject,
    string Body,
    Dictionary<string, string>? Data = null,
    NotificationPriority Priority = NotificationPriority.Normal,
    string? EntityType = null,
    string? EntityId = null);

namespace Spydersoft.Notification.Contracts;

public sealed record NotificationDto(
    Guid Id,
    string UserId,
    string Source,
    string Type,
    string Subject,
    string Body,
    Dictionary<string, string>? Data,
    NotificationPriority Priority,
    NotificationStatus Status,
    bool IsRead,
    DateTimeOffset? ReadAt,
    DateTimeOffset CreatedAt,
    string? EntityType,
    string? EntityId,
    IReadOnlyList<NotificationDeliveryDto>? Deliveries = null);

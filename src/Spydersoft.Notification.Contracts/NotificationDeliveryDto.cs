namespace Spydersoft.Notification.Contracts;

public sealed record NotificationDeliveryDto(
    NotificationChannel Channel,
    DeliveryStatus Status,
    string? ExternalId,
    string? Error,
    DateTimeOffset? AttemptedAt);

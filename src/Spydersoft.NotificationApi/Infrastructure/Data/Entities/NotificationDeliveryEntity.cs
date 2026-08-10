using Spydersoft.Notification.Contracts;

namespace Spydersoft.NotificationApi.Infrastructure.Data.Entities;

public sealed class NotificationDeliveryEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid NotificationId { get; set; }
    public NotificationChannel Channel { get; set; }
    public DeliveryStatus Status { get; set; } = DeliveryStatus.Pending;
    public string? ExternalId { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset? AttemptedAt { get; set; }

    public NotificationEntity? Notification { get; set; }
}

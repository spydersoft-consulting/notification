using Spydersoft.Notification.Contracts;

namespace Spydersoft.NotificationApi.Infrastructure.Data.Entities;

public sealed class NotificationEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Dictionary<string, string>? Data { get; set; }
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public NotificationStatus Status { get; set; } = NotificationStatus.Created;
    public bool IsRead { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }

    public ICollection<NotificationDeliveryEntity> Deliveries { get; set; } = [];
}

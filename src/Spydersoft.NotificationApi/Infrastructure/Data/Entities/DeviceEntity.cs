using Spydersoft.Notification.Contracts;

namespace Spydersoft.NotificationApi.Infrastructure.Data.Entities;

public sealed class DeviceEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public DeviceType DeviceType { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? PushToken { get; set; }
    public DateTimeOffset LastSeenAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset RegisteredAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsActive { get; set; } = true;
}

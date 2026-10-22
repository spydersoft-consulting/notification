namespace Spydersoft.NotificationApi.Infrastructure.Data.Entities;

public sealed class NotificationPreferenceEntity
{
    public string UserId { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool SmsOptOut { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

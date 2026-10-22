namespace Spydersoft.Notification.Contracts;

public sealed record NotificationPreferenceDto(
    string? Email,
    string? PhoneNumber,
    bool SmsOptOut,
    IReadOnlyList<NotificationTypePreferenceDto> TypePreferences);

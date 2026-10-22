namespace Spydersoft.Notification.Contracts;

public sealed record NotificationTypePreferenceDto(
    string Source,
    string Type,
    bool EmailEnabled,
    bool SmsEnabled);

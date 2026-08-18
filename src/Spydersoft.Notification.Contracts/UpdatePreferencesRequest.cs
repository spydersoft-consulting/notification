namespace Spydersoft.Notification.Contracts;

public sealed record UpdatePreferencesRequest(string? Email, string? PhoneNumber, bool SmsOptOut);

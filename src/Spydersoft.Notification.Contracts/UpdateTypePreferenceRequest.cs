namespace Spydersoft.Notification.Contracts;

public sealed record UpdateTypePreferenceRequest(bool EmailEnabled, bool SmsEnabled);

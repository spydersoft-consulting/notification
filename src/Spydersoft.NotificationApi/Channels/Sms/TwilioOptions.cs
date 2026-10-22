namespace Spydersoft.NotificationApi.Channels.Sms;

public sealed class TwilioOptions
{
    public const string SectionName = "Twilio";

    public string? AccountSid { get; set; }
    public string? AuthToken { get; set; }
    public string? FromNumber { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(AccountSid) && !string.IsNullOrWhiteSpace(AuthToken) && !string.IsNullOrWhiteSpace(FromNumber);
}

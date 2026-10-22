namespace Spydersoft.NotificationApi.Channels.Sms;

public interface ITwilioGateway
{
    Task<TwilioSendResult> SendAsync(string toPhoneNumber, string body, CancellationToken cancellationToken = default);
}

public sealed record TwilioSendResult(bool Success, string? MessageSid, string? Error);

using Spydersoft.Notification.Contracts;

namespace Spydersoft.NotificationApi.Channels.Sms;

public sealed class SmsChannelSender(ITwilioGateway twilio) : INotificationChannelSender
{
    internal const int MaxBodyLength = 1600;

    public NotificationChannel Channel => NotificationChannel.Sms;

    public async Task<ChannelSendResult> SendAsync(NotificationPushDto notification, string destination, CancellationToken cancellationToken = default)
    {
        var body = Truncate($"{notification.Subject}\n{notification.Body}", MaxBodyLength);
        var result = await twilio.SendAsync(destination, body, cancellationToken);
        return new ChannelSendResult(result.Success, result.MessageSid, result.Error);
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..(max - 1)] + "…";
}

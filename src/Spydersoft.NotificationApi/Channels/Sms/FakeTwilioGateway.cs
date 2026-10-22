using System.Collections.Concurrent;

namespace Spydersoft.NotificationApi.Channels.Sms;

public sealed class FakeTwilioGateway : ITwilioGateway
{
    private readonly ConcurrentQueue<SentSms> _sent = new();

    public IReadOnlyCollection<SentSms> Sent => _sent.ToArray();

    public Task<TwilioSendResult> SendAsync(string toPhoneNumber, string body, CancellationToken cancellationToken = default)
    {
        var sid = $"SMFAKE{Guid.NewGuid():N}";
        _sent.Enqueue(new SentSms(toPhoneNumber, body, sid));
        return Task.FromResult(new TwilioSendResult(true, sid, null));
    }
}

public sealed record SentSms(string To, string Body, string MessageSid);

using Spydersoft.Notification.Contracts;
using Spydersoft.NotificationApi.Channels.Sms;

namespace Spydersoft.NotificationApi.UnitTests.Channels;

[TestFixture]
public sealed class SmsChannelSenderTests
{
    [Test]
    public async Task SendAsync_FoldsSubjectIntoBody_AndPassesDestination()
    {
        var gateway = new FakeTwilioGateway();
        var sut = new SmsChannelSender(gateway);

        var result = await sut.SendAsync(Push("Recall", "Brake issue"), "+15551234567");

        Assert.That(result.Success, Is.True);
        var sent = gateway.Sent.Single();
        Assert.That(sent.To, Is.EqualTo("+15551234567"));
        Assert.That(sent.Body, Is.EqualTo("Recall\nBrake issue"));
        Assert.That(result.ExternalId, Is.EqualTo(sent.MessageSid));
    }

    [Test]
    public async Task SendAsync_LongBody_TruncatedTo1600()
    {
        var gateway = new FakeTwilioGateway();
        var sut = new SmsChannelSender(gateway);

        await sut.SendAsync(Push("S", new string('x', 3000)), "+15551234567");

        var body = gateway.Sent.Single().Body;
        Assert.That(body, Has.Length.EqualTo(1600));
        Assert.That(body, Does.EndWith("…"));
    }

    [Test]
    public async Task SendAsync_GatewayFailure_MapsToFailedResult()
    {
        var sut = new SmsChannelSender(new FailingGateway());

        var result = await sut.SendAsync(Push("S", "B"), "+15551234567");

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("boom"));
    }

    [Test]
    public async Task FakeTwilioGateway_RecordsEachSend_WithUniqueSids()
    {
        var gateway = new FakeTwilioGateway();

        var a = await gateway.SendAsync("+15551234567", "one");
        var b = await gateway.SendAsync("+15557654321", "two");

        Assert.That(gateway.Sent, Has.Count.EqualTo(2));
        Assert.That(a.MessageSid, Is.Not.EqualTo(b.MessageSid));
    }

    private static NotificationPushDto Push(string subject, string body) =>
        new(Guid.NewGuid(), "pitstop", "recall-alert", subject, body, NotificationPriority.High, DateTimeOffset.UtcNow);

    private sealed class FailingGateway : ITwilioGateway
    {
        public Task<TwilioSendResult> SendAsync(string toPhoneNumber, string body, CancellationToken cancellationToken = default) =>
            Task.FromResult(new TwilioSendResult(false, null, "boom"));
    }
}

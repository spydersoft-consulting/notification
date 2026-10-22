using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Spydersoft.Notification.Contracts;
using Spydersoft.NotificationApi.Channels;
using Spydersoft.NotificationApi.Hub;
using Spydersoft.NotificationApi.Infrastructure.Data;
using Spydersoft.NotificationApi.Infrastructure.Data.Entities;
using Spydersoft.NotificationApi.Routing;

namespace Spydersoft.NotificationApi.UnitTests.Routing;

[TestFixture]
public sealed class NotificationRouterTests
{
    [Test]
    public async Task DispatchAsync_HubPushSucceeds_InAppDeliveryIsSent_OtherChannelsSkipped()
    {
        using var db = CreateDb();
        var notification = SeedNotification(db);
        var hub = Substitute.For<IHubPushClient>();
        hub.PushAsync(notification.UserId, Arg.Any<NotificationPushDto>(), Arg.Any<CancellationToken>()).Returns(true);

        var sut = new NotificationRouter(db, hub, []);
        await sut.DispatchAsync(notification.Id);

        var updated = await db.Notifications.Include(n => n.Deliveries).FirstAsync(n => n.Id == notification.Id);
        Assert.That(updated.Status, Is.EqualTo(NotificationStatus.Dispatched));

        var inApp = updated.Deliveries.Single(d => d.Channel == NotificationChannel.InApp);
        Assert.That(inApp.Status, Is.EqualTo(DeliveryStatus.Sent));

        Assert.That(updated.Deliveries.Single(d => d.Channel == NotificationChannel.Email).Status, Is.EqualTo(DeliveryStatus.Skipped));
        Assert.That(updated.Deliveries.Single(d => d.Channel == NotificationChannel.Sms).Status, Is.EqualTo(DeliveryStatus.Skipped));
    }

    [Test]
    public async Task DispatchAsync_HubPushFails_InAppDeliveryFailed_StatusPartiallyFailed()
    {
        using var db = CreateDb();
        var notification = SeedNotification(db);
        var hub = Substitute.For<IHubPushClient>();
        hub.PushAsync(notification.UserId, Arg.Any<NotificationPushDto>(), Arg.Any<CancellationToken>()).Returns(false);

        var sut = new NotificationRouter(db, hub, []);
        await sut.DispatchAsync(notification.Id);

        var updated = await db.Notifications.Include(n => n.Deliveries).FirstAsync(n => n.Id == notification.Id);
        Assert.That(updated.Status, Is.EqualTo(NotificationStatus.PartiallyFailed));
        Assert.That(updated.Deliveries.Single(d => d.Channel == NotificationChannel.InApp).Status, Is.EqualTo(DeliveryStatus.Failed));
    }

    [Test]
    public async Task DispatchAsync_AlreadyDispatched_IsNoOp()
    {
        using var db = CreateDb();
        var notification = SeedNotification(db, NotificationStatus.Dispatched);
        var hub = Substitute.For<IHubPushClient>();

        var sut = new NotificationRouter(db, hub, []);
        await sut.DispatchAsync(notification.Id);

        await hub.DidNotReceive().PushAsync(Arg.Any<string>(), Arg.Any<NotificationPushDto>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DispatchAsync_ReDispatch_AlreadySentInAppChannel_IsNotResent()
    {
        using var db = CreateDb();
        var notification = SeedNotification(db, NotificationStatus.PartiallyFailed);
        var existingDelivery = new NotificationDeliveryEntity
        {
            NotificationId = notification.Id,
            Channel = NotificationChannel.InApp,
            Status = DeliveryStatus.Sent,
            AttemptedAt = DateTimeOffset.UtcNow,
        };
        notification.Deliveries.Add(existingDelivery);
        db.NotificationDeliveries.Add(existingDelivery);
        await db.SaveChangesAsync();

        var hub = Substitute.For<IHubPushClient>();
        var sut = new NotificationRouter(db, hub, []);
        await sut.DispatchAsync(notification.Id);

        await hub.DidNotReceive().PushAsync(Arg.Any<string>(), Arg.Any<NotificationPushDto>(), Arg.Any<CancellationToken>());

        var updated = await db.Notifications.Include(n => n.Deliveries).FirstAsync(n => n.Id == notification.Id);
        Assert.That(updated.Status, Is.EqualTo(NotificationStatus.Dispatched));
    }

    [Test]
    public async Task DispatchAsync_EmailPreferenceSet_EmailSenderCalledWithAddress()
    {
        using var db = CreateDb();
        var notification = SeedNotification(db);
        SeedPrefs(db, email: "a@b.com");
        var email = Sender(NotificationChannel.Email);

        await CreateSut(db, email).DispatchAsync(notification.Id);

        await email.Received(1).SendAsync(Arg.Any<NotificationPushDto>(), "a@b.com", Arg.Any<CancellationToken>());
        var updated = await Reload(db, notification);
        Assert.That(updated.Status, Is.EqualTo(NotificationStatus.Dispatched));
        Assert.That(updated.Deliveries.Single(d => d.Channel == NotificationChannel.Email).Status, Is.EqualTo(DeliveryStatus.Sent));
    }

    [Test]
    public async Task DispatchAsync_NoEmailOnFile_EmailSkipped_StatusDispatched()
    {
        using var db = CreateDb();
        var notification = SeedNotification(db);
        var email = Sender(NotificationChannel.Email);

        await CreateSut(db, email).DispatchAsync(notification.Id);

        await email.DidNotReceive().SendAsync(Arg.Any<NotificationPushDto>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        var updated = await Reload(db, notification);
        Assert.That(updated.Status, Is.EqualTo(NotificationStatus.Dispatched));
        Assert.That(updated.Deliveries.Single(d => d.Channel == NotificationChannel.Email).Status, Is.EqualTo(DeliveryStatus.Skipped));
    }

    [Test]
    public async Task DispatchAsync_TypeEmailDisabled_EmailSkipped()
    {
        using var db = CreateDb();
        var notification = SeedNotification(db);
        SeedPrefs(db, email: "a@b.com");
        db.NotificationTypePreferences.Add(new NotificationTypePreferenceEntity { UserId = "user-1", Source = "pitstop", Type = "recall-alert", EmailEnabled = false });
        db.SaveChanges();
        var email = Sender(NotificationChannel.Email);

        await CreateSut(db, email).DispatchAsync(notification.Id);

        await email.DidNotReceive().SendAsync(Arg.Any<NotificationPushDto>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [TestCase(NotificationPriority.Normal, false, false, "+15551234567", false)]
    [TestCase(NotificationPriority.Normal, true, false, "+15551234567", true)]
    [TestCase(NotificationPriority.Low, true, false, null, false)]
    [TestCase(NotificationPriority.High, false, false, "+15551234567", true)]
    [TestCase(NotificationPriority.High, false, false, null, false)]
    [TestCase(NotificationPriority.High, false, true, "+15551234567", false)]
    public async Task DispatchAsync_SmsResolution(NotificationPriority priority, bool typeSmsEnabled, bool optOut, string? phone, bool expectSent)
    {
        using var db = CreateDb();
        var notification = SeedNotification(db);
        notification.Priority = priority;
        SeedPrefs(db, phone: phone, smsOptOut: optOut);
        db.NotificationTypePreferences.Add(new NotificationTypePreferenceEntity { UserId = "user-1", Source = "pitstop", Type = "recall-alert", SmsEnabled = typeSmsEnabled });
        db.SaveChanges();
        var sms = Sender(NotificationChannel.Sms);

        await CreateSut(db, sms).DispatchAsync(notification.Id);

        if (expectSent)
        {
            await sms.Received(1).SendAsync(Arg.Any<NotificationPushDto>(), phone!, Arg.Any<CancellationToken>());
        }
        else
        {
            await sms.DidNotReceive().SendAsync(Arg.Any<NotificationPushDto>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        }
    }

    [Test]
    public async Task DispatchAsync_SenderFails_StatusPartiallyFailed_OtherChannelStillSent()
    {
        using var db = CreateDb();
        var notification = SeedNotification(db);
        SeedPrefs(db, email: "a@b.com");
        var email = Sender(NotificationChannel.Email, success: false);

        await CreateSut(db, email).DispatchAsync(notification.Id);

        var updated = await Reload(db, notification);
        Assert.That(updated.Status, Is.EqualTo(NotificationStatus.PartiallyFailed));
        Assert.That(updated.Deliveries.Single(d => d.Channel == NotificationChannel.InApp).Status, Is.EqualTo(DeliveryStatus.Sent));
        Assert.That(updated.Deliveries.Single(d => d.Channel == NotificationChannel.Email).Status, Is.EqualTo(DeliveryStatus.Failed));
    }

    [Test]
    public async Task DispatchAsync_SenderThrows_TreatedAsFailed()
    {
        using var db = CreateDb();
        var notification = SeedNotification(db);
        SeedPrefs(db, email: "a@b.com");
        var email = Sender(NotificationChannel.Email);
        email.SendAsync(Arg.Any<NotificationPushDto>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<ChannelSendResult>(_ => throw new InvalidOperationException());

        await CreateSut(db, email).DispatchAsync(notification.Id);

        Assert.That((await Reload(db, notification)).Status, Is.EqualTo(NotificationStatus.PartiallyFailed));
    }

    [Test]
    public async Task DispatchAsync_ReDispatch_OnlyFailedChannelRetried()
    {
        using var db = CreateDb();
        var notification = SeedNotification(db, NotificationStatus.PartiallyFailed);
        notification.Priority = NotificationPriority.High;
        SeedPrefs(db, email: "a@b.com", phone: "+15551234567");
        foreach (var (channel, status) in new[] { (NotificationChannel.InApp, DeliveryStatus.Sent), (NotificationChannel.Email, DeliveryStatus.Sent), (NotificationChannel.Sms, DeliveryStatus.Failed) })
        {
            var d = new NotificationDeliveryEntity { NotificationId = notification.Id, Channel = channel, Status = status };
            notification.Deliveries.Add(d);
            db.NotificationDeliveries.Add(d);
        }
        db.SaveChanges();
        var email = Sender(NotificationChannel.Email);
        var sms = Sender(NotificationChannel.Sms);

        await CreateSut(db, email, sms).DispatchAsync(notification.Id);

        await email.DidNotReceive().SendAsync(Arg.Any<NotificationPushDto>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await sms.Received(1).SendAsync(Arg.Any<NotificationPushDto>(), "+15551234567", Arg.Any<CancellationToken>());
        Assert.That((await Reload(db, notification)).Status, Is.EqualTo(NotificationStatus.Dispatched));
    }

    private static NotificationRouter CreateSut(NotificationDbContext db, params INotificationChannelSender[] senders)
    {
        var hub = Substitute.For<IHubPushClient>();
        hub.PushAsync(Arg.Any<string>(), Arg.Any<NotificationPushDto>(), Arg.Any<CancellationToken>()).Returns(true);
        return new NotificationRouter(db, hub, senders);
    }

    private static INotificationChannelSender Sender(NotificationChannel channel, bool success = true)
    {
        var sender = Substitute.For<INotificationChannelSender>();
        sender.Channel.Returns(channel);
        sender.SendAsync(Arg.Any<NotificationPushDto>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ChannelSendResult(success, null, success ? null : "boom"));
        return sender;
    }

    private static void SeedPrefs(NotificationDbContext db, string? email = null, string? phone = null, bool smsOptOut = false)
    {
        db.NotificationPreferences.Add(new NotificationPreferenceEntity { UserId = "user-1", Email = email, PhoneNumber = phone, SmsOptOut = smsOptOut });
        db.SaveChanges();
    }

    private static Task<NotificationEntity> Reload(NotificationDbContext db, NotificationEntity n) =>
        db.Notifications.AsNoTracking().Include(x => x.Deliveries).FirstAsync(x => x.Id == n.Id);

    private static NotificationEntity SeedNotification(NotificationDbContext db, NotificationStatus status = NotificationStatus.Created)
    {
        var notification = new NotificationEntity
        {
            UserId = "user-1",
            Source = "pitstop",
            Type = "recall-alert",
            Subject = "Subject",
            Body = "Body",
            Status = status,
        };
        db.Notifications.Add(notification);
        db.SaveChanges();
        return notification;
    }

    private static NotificationDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<NotificationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}

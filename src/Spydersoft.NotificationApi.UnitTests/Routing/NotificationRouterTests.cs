using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Spydersoft.Notification.Contracts;
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

        var sut = new NotificationRouter(db, hub);
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

        var sut = new NotificationRouter(db, hub);
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

        var sut = new NotificationRouter(db, hub);
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
        var sut = new NotificationRouter(db, hub);
        await sut.DispatchAsync(notification.Id);

        await hub.DidNotReceive().PushAsync(Arg.Any<string>(), Arg.Any<NotificationPushDto>(), Arg.Any<CancellationToken>());

        var updated = await db.Notifications.Include(n => n.Deliveries).FirstAsync(n => n.Id == notification.Id);
        Assert.That(updated.Status, Is.EqualTo(NotificationStatus.Dispatched));
    }

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

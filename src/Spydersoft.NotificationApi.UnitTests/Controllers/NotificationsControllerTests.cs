using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spydersoft.Notification.Contracts;
using Spydersoft.NotificationApi.Controllers;
using Spydersoft.NotificationApi.Dispatch;
using Spydersoft.NotificationApi.Infrastructure.Data;

namespace Spydersoft.NotificationApi.UnitTests.Controllers;

[TestFixture]
public sealed class NotificationsControllerTests
{
    [Test]
    public async Task Create_PersistsNotification_AndEnqueuesDispatch()
    {
        using var db = CreateDb();
        var queue = new NotificationDispatchQueue();
        var controller = CreateController(db, queue, "user-a");

        var request = new CreateNotificationRequest("user-a", "pitstop", "recall-alert", "Subject", "Body");
        var result = await controller.Create(request, CancellationToken.None);

        var created = (CreatedResult)result;
        var dto = (NotificationDto)created.Value!;
        Assert.That(dto.Status, Is.EqualTo(NotificationStatus.Created));
        Assert.That(await db.Notifications.CountAsync(), Is.EqualTo(1));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var enumerator = queue.ReadAllAsync(cts.Token).GetAsyncEnumerator(cts.Token);
        Assert.That(await enumerator.MoveNextAsync(), Is.True);
    }

    [Test]
    public async Task Create_MissingRequiredField_ReturnsBadRequest()
    {
        using var db = CreateDb();
        var controller = CreateController(db, new NotificationDispatchQueue(), "user-a");

        var request = new CreateNotificationRequest("user-a", "", "recall-alert", "Subject", "Body");
        var result = await controller.Create(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
    }

    [Test]
    public async Task List_OnlyReturnsCallersOwnNotifications()
    {
        using var db = CreateDb();
        db.Notifications.AddRange(
            NewNotification("user-a"),
            NewNotification("user-b"));
        await db.SaveChangesAsync();

        var controller = CreateController(db, new NotificationDispatchQueue(), "user-a");
        var result = (OkObjectResult)await controller.List(unreadOnly: false, source: null, type: null);

        var list = (IEnumerable<NotificationDto>)result.Value!;
        Assert.That(list.Select(n => n.UserId), Is.All.EqualTo("user-a"));
    }

    [Test]
    public async Task MarkRead_SetsIsReadAndReadAt_IsIdempotent()
    {
        using var db = CreateDb();
        var notification = NewNotification("user-a");
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        var controller = CreateController(db, new NotificationDispatchQueue(), "user-a");

        var first = (OkObjectResult)await controller.MarkRead(notification.Id, CancellationToken.None);
        var firstDto = (NotificationDto)first.Value!;
        Assert.That(firstDto.IsRead, Is.True);
        Assert.That(firstDto.ReadAt, Is.Not.Null);

        var second = (OkObjectResult)await controller.MarkRead(notification.Id, CancellationToken.None);
        Assert.That(((NotificationDto)second.Value!).IsRead, Is.True);
    }

    [Test]
    public async Task MarkAllRead_UpdatesOnlyCallersUnreadNotifications()
    {
        using var db = CreateDb();
        db.Notifications.AddRange(
            NewNotification("user-a"),
            NewNotification("user-a"),
            NewNotification("user-b"));
        await db.SaveChangesAsync();

        var controller = CreateController(db, new NotificationDispatchQueue(), "user-a");
        var result = (OkObjectResult)await controller.MarkAllRead(CancellationToken.None);

        dynamic body = result.Value!;
        Assert.That((int)body.updatedCount, Is.EqualTo(2));
        Assert.That(await db.Notifications.CountAsync(n => n.UserId == "user-b" && !n.IsRead), Is.EqualTo(1));
    }

    [Test]
    public async Task UnreadCount_ReflectsUnreadNotificationsForCaller()
    {
        using var db = CreateDb();
        var read = NewNotification("user-a");
        read.IsRead = true;
        db.Notifications.AddRange(read, NewNotification("user-a"), NewNotification("user-a"));
        await db.SaveChangesAsync();

        var controller = CreateController(db, new NotificationDispatchQueue(), "user-a");
        var result = (OkObjectResult)await controller.UnreadCount(CancellationToken.None);

        dynamic body = result.Value!;
        Assert.That((int)body.count, Is.EqualTo(2));
    }

    [Test]
    public async Task Get_OtherUsersNotification_ReturnsNotFound()
    {
        using var db = CreateDb();
        var notification = NewNotification("user-b");
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        var controller = CreateController(db, new NotificationDispatchQueue(), "user-a");
        var result = await controller.Get(notification.Id, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    private static Infrastructure.Data.Entities.NotificationEntity NewNotification(string userId) => new()
    {
        UserId = userId,
        Source = "pitstop",
        Type = "recall-alert",
        Subject = "Subject",
        Body = "Body",
    };

    private static NotificationDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<NotificationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static NotificationsController CreateController(NotificationDbContext db, NotificationDispatchQueue queue, string userId)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId)], "test"));
        return new NotificationsController(db, queue)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = principal } },
        };
    }
}

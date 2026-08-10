using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Spydersoft.Notification.Contracts;
using Spydersoft.NotificationApi.Dispatch;
using Spydersoft.NotificationApi.Infrastructure.Data;
using Spydersoft.NotificationApi.Infrastructure.Data.Entities;

namespace Spydersoft.NotificationApi.UnitTests.Dispatch;

[TestFixture]
public sealed class DispatchReconciliationServiceTests
{
    [Test]
    public async Task SweepAsync_StaleCreatedNotification_IsReenqueued()
    {
        var dbName = Guid.NewGuid().ToString();
        using var provider = BuildProvider(dbName);
        using (var db = CreateDb(dbName))
        {
            db.Notifications.Add(CreateNotification(NotificationStatus.Created, DateTimeOffset.UtcNow.AddMinutes(-5)));
            await db.SaveChangesAsync();
        }

        var queue = new NotificationDispatchQueue();
        var sut = new DispatchReconciliationService(queue, provider.GetRequiredService<IServiceScopeFactory>());

        await sut.SweepAsync(CancellationToken.None);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var enumerator = queue.ReadAllAsync(cts.Token).GetAsyncEnumerator(cts.Token);
        Assert.That(await enumerator.MoveNextAsync(), Is.True);
    }

    [Test]
    public async Task SweepAsync_FreshCreatedNotification_IsNotReenqueued()
    {
        var dbName = Guid.NewGuid().ToString();
        using var provider = BuildProvider(dbName);
        using (var db = CreateDb(dbName))
        {
            db.Notifications.Add(CreateNotification(NotificationStatus.Created, DateTimeOffset.UtcNow.AddSeconds(-30)));
            await db.SaveChangesAsync();
        }

        var queue = new NotificationDispatchQueue();
        var sut = new DispatchReconciliationService(queue, provider.GetRequiredService<IServiceScopeFactory>());

        await sut.SweepAsync(CancellationToken.None);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        var enumerator = queue.ReadAllAsync(cts.Token).GetAsyncEnumerator(cts.Token);
        Assert.ThrowsAsync<OperationCanceledException>(async () => await enumerator.MoveNextAsync());
    }

    private static NotificationEntity CreateNotification(NotificationStatus status, DateTimeOffset createdAt) => new()
    {
        UserId = "user-1",
        Source = "pitstop",
        Type = "recall-alert",
        Subject = "Subject",
        Body = "Body",
        Status = status,
        CreatedAt = createdAt,
    };

    private static NotificationDbContext CreateDb(string name) =>
        new(new DbContextOptionsBuilder<NotificationDbContext>().UseInMemoryDatabase(name).Options);

    private static ServiceProvider BuildProvider(string dbName)
    {
        var services = new ServiceCollection();
        services.AddDbContext<NotificationDbContext>(o => o.UseInMemoryDatabase(dbName));
        return services.BuildServiceProvider();
    }
}

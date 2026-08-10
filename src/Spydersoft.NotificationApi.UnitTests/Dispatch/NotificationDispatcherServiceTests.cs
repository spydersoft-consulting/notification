using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Spydersoft.NotificationApi.Dispatch;
using Spydersoft.NotificationApi.Routing;

namespace Spydersoft.NotificationApi.UnitTests.Dispatch;

[TestFixture]
public sealed class NotificationDispatcherServiceTests
{
    [Test]
    public async Task ExecuteAsync_EnqueuedItem_InvokesRouterDispatchAsync()
    {
        var queue = new NotificationDispatchQueue();
        var router = Substitute.For<INotificationRouter>();
        using var provider = BuildProvider(router);

        var sut = new NotificationDispatcherService(queue, provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<NotificationDispatcherService>.Instance);

        var notificationId = Guid.NewGuid();
        await queue.EnqueueAsync(new DispatchItem(notificationId));

        await sut.StartAsync(CancellationToken.None);
        await WaitUntil(() => router.ReceivedCalls().Any());
        await sut.StopAsync(CancellationToken.None);

        await router.Received(1).DispatchAsync(notificationId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExecuteAsync_ThrowingRouter_DoesNotStopProcessingSubsequentItems()
    {
        var queue = new NotificationDispatchQueue();
        var router = Substitute.For<INotificationRouter>();
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        router.DispatchAsync(firstId, Arg.Any<CancellationToken>()).Returns<Task>(_ => throw new InvalidOperationException("boom"));

        using var provider = BuildProvider(router);
        var sut = new NotificationDispatcherService(queue, provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<NotificationDispatcherService>.Instance);

        await queue.EnqueueAsync(new DispatchItem(firstId));
        await queue.EnqueueAsync(new DispatchItem(secondId));

        await sut.StartAsync(CancellationToken.None);
        await WaitUntil(() => router.ReceivedCalls().Count() >= 2);
        await sut.StopAsync(CancellationToken.None);

        await router.Received(1).DispatchAsync(secondId, Arg.Any<CancellationToken>());
    }

    private static ServiceProvider BuildProvider(INotificationRouter router)
    {
        var services = new ServiceCollection();
        services.AddSingleton(router);
        return services.BuildServiceProvider();
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!condition())
        {
            if (cts.IsCancellationRequested)
            {
                Assert.Fail("Timed out waiting for the expected condition.");
            }
            await Task.Delay(20);
        }
    }
}

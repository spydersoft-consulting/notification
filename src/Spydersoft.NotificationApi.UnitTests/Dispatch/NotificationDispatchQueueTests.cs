using Spydersoft.NotificationApi.Dispatch;

namespace Spydersoft.NotificationApi.UnitTests.Dispatch;

[TestFixture]
public sealed class NotificationDispatchQueueTests
{
    [Test]
    public async Task EnqueueAsync_ItemIsYieldedByReadAllAsync()
    {
        var queue = new NotificationDispatchQueue();
        var item = new DispatchItem(Guid.NewGuid());

        await queue.EnqueueAsync(item);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await foreach (var read in queue.ReadAllAsync(cts.Token))
        {
            Assert.That(read, Is.EqualTo(item));
            return;
        }

        Assert.Fail("Expected an item to be read from the queue.");
    }
}

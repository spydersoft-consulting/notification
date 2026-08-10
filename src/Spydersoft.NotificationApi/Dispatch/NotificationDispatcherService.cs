using Spydersoft.NotificationApi.Routing;

namespace Spydersoft.NotificationApi.Dispatch;

public sealed class NotificationDispatcherService(
    NotificationDispatchQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<NotificationDispatcherService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var item in queue.ReadAllAsync(stoppingToken))
        {
            using var scope = scopeFactory.CreateScope();
            var router = scope.ServiceProvider.GetRequiredService<INotificationRouter>();
            try
            {
                await router.DispatchAsync(item.NotificationId, stoppingToken);
            }
            catch (Exception ex)
            {
                // The router already persists per-channel Failed status internally; this catch
                // is a last-resort guard so one bad notification can't kill the loop.
                logger.LogError(ex, "Unhandled dispatch failure for {NotificationId}", item.NotificationId);
            }
        }
    }
}

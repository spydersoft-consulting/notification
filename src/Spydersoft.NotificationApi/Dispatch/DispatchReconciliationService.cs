using Microsoft.EntityFrameworkCore;
using Spydersoft.Notification.Contracts;
using Spydersoft.NotificationApi.Infrastructure.Data;

namespace Spydersoft.NotificationApi.Dispatch;

/// <summary>
/// Backstop for the in-memory dispatch queue: re-enqueues notifications stuck in Created/Dispatching
/// past a grace window, covering the case where the process restarted between enqueue and dispatch.
/// See plans/notifications/dispatch-spec.md#durability-trade-off.
/// </summary>
public sealed class DispatchReconciliationService(
    NotificationDispatchQueue queue,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    internal static readonly TimeSpan SweepInterval = TimeSpan.FromSeconds(60);
    internal static readonly TimeSpan StaleThreshold = TimeSpan.FromMinutes(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(SweepInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await SweepAsync(stoppingToken);
        }
    }

    internal async Task SweepAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

        var cutoff = DateTimeOffset.UtcNow - StaleThreshold;
        var stale = await db.Notifications
            .Where(n => (n.Status == NotificationStatus.Created || n.Status == NotificationStatus.Dispatching)
                        && n.CreatedAt < cutoff)
            .Select(n => n.Id)
            .ToListAsync(cancellationToken);

        foreach (var id in stale)
        {
            await queue.EnqueueAsync(new DispatchItem(id), cancellationToken);
        }
    }
}

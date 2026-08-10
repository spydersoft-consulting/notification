using Microsoft.EntityFrameworkCore;
using Spydersoft.Notification.Contracts;
using Spydersoft.NotificationApi.Hub;
using Spydersoft.NotificationApi.Infrastructure.Data;
using Spydersoft.NotificationApi.Infrastructure.Data.Entities;

namespace Spydersoft.NotificationApi.Routing;

/// <summary>
/// Channel selection and fan-out. v1 resolves InApp only — no preference lookup exists yet
/// (there's nothing to look up until email/SMS preferences ship). The shape here (existence
/// check before send, Skipped rows for unresolved channels, status rollup) is built generally
/// so Email/Sms resolution can be added later without a rewrite. See
/// plans/notifications/router-spec.md.
/// </summary>
public sealed class NotificationRouter(NotificationDbContext db, IHubPushClient hubPushClient) : INotificationRouter
{
    public async Task DispatchAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await db.Notifications
            .Include(n => n.Deliveries)
            .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);

        if (notification is null || notification.Status == NotificationStatus.Dispatched)
        {
            // Already fully dispatched (or gone) — a reconciliation-sweep re-enqueue racing a
            // completed dispatch is a no-op.
            return;
        }

        notification.Status = NotificationStatus.Dispatching;
        await db.SaveChangesAsync(cancellationToken);

        var resolvedChannels = ResolveChannels(notification);

        var attemptedResults = new List<bool>();

        foreach (var channel in resolvedChannels)
        {
            var delivery = notification.Deliveries.FirstOrDefault(d => d.Channel == channel);
            if (delivery is { Status: DeliveryStatus.Sent })
            {
                // Idempotent re-dispatch: already succeeded on this channel, don't resend.
                attemptedResults.Add(true);
                continue;
            }

            delivery ??= AddDelivery(notification, channel, DeliveryStatus.Pending);

            var sent = await SendAsync(channel, notification, cancellationToken);
            delivery.Status = sent ? DeliveryStatus.Sent : DeliveryStatus.Failed;
            delivery.AttemptedAt = DateTimeOffset.UtcNow;
            attemptedResults.Add(sent);
        }

        foreach (var channel in Enum.GetValues<NotificationChannel>().Except(resolvedChannels))
        {
            if (notification.Deliveries.Any(d => d.Channel == channel))
            {
                continue;
            }

            AddDelivery(notification, channel, DeliveryStatus.Skipped);
        }

        notification.Status = attemptedResults.TrueForAll(r => r)
            ? NotificationStatus.Dispatched
            : NotificationStatus.PartiallyFailed;

        await db.SaveChangesAsync(cancellationToken);
    }

    private NotificationDeliveryEntity AddDelivery(NotificationEntity notification, NotificationChannel channel, DeliveryStatus status)
    {
        var delivery = new NotificationDeliveryEntity
        {
            NotificationId = notification.Id,
            Channel = channel,
            Status = status,
        };
        notification.Deliveries.Add(delivery);
        // Client-generated Guid keys set before the entity reaches the change tracker are
        // otherwise inferred as "existing" via graph fixup alone — explicitly Add() so EF
        // tracks it as a new row instead of attempting an Update against a row that doesn't exist.
        db.NotificationDeliveries.Add(delivery);
        return delivery;
    }

    private async Task<bool> SendAsync(NotificationChannel channel, NotificationEntity notification, CancellationToken cancellationToken)
    {
        return channel switch
        {
            NotificationChannel.InApp => await hubPushClient.PushAsync(
                notification.UserId,
                new NotificationPushDto(notification.Id, notification.Source, notification.Type, notification.Subject, notification.Body, notification.Priority, notification.CreatedAt),
                cancellationToken),
            // Email/Sms senders ship with the preferences/channel phases — not part of v1.
            _ => false,
        };
    }

    /// <summary>
    /// v1: InApp is always attempted — it's free and the hub itself degrades to Skipped when the
    /// user has no active connection, so there's no reason to gate it on preference. Email/Sms
    /// resolution is added once preferences exist (see plans/notifications/router-spec.md).
    /// </summary>
    private static IReadOnlySet<NotificationChannel> ResolveChannels(NotificationEntity notification) =>
        new HashSet<NotificationChannel> { NotificationChannel.InApp };
}

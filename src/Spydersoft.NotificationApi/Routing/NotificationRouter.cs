using Microsoft.EntityFrameworkCore;
using Spydersoft.Notification.Contracts;
using Spydersoft.NotificationApi.Channels;
using Spydersoft.NotificationApi.Hub;
using Spydersoft.NotificationApi.Infrastructure.Data;
using Spydersoft.NotificationApi.Infrastructure.Data.Entities;

namespace Spydersoft.NotificationApi.Routing;

/// <summary>
/// Channel selection and fan-out. Channels resolve from the user's preferences; a channel with
/// no registered <see cref="INotificationChannelSender"/> resolves as Skipped. See
/// plans/notifications/router-spec.md.
/// </summary>
public sealed class NotificationRouter(
    NotificationDbContext db,
    IHubPushClient hubPushClient,
    IEnumerable<INotificationChannelSender> senders) : INotificationRouter
{
    private readonly Dictionary<NotificationChannel, INotificationChannelSender> _senders =
        senders.ToDictionary(s => s.Channel);

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

        var prefs = await db.NotificationPreferences.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == notification.UserId, cancellationToken);
        var typePref = await db.NotificationTypePreferences.AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.UserId == notification.UserId && p.Source == notification.Source && p.Type == notification.Type,
                cancellationToken);

        var resolvedChannels = ResolveChannels(notification, prefs, typePref);

        var pending = new List<(NotificationDeliveryEntity Delivery, Task<bool> Send)>();
        var alreadySent = 0;

        foreach (var channel in resolvedChannels)
        {
            var delivery = notification.Deliveries.FirstOrDefault(d => d.Channel == channel);
            if (delivery is { Status: DeliveryStatus.Sent })
            {
                // Idempotent re-dispatch: already succeeded on this channel, don't resend.
                alreadySent++;
                continue;
            }

            delivery ??= AddDelivery(notification, channel, DeliveryStatus.Pending);
            pending.Add((delivery, SendAsync(channel, notification, prefs, cancellationToken)));
        }

        foreach (var channel in Enum.GetValues<NotificationChannel>().Except(resolvedChannels))
        {
            if (notification.Deliveries.Any(d => d.Channel == channel))
            {
                continue;
            }

            AddDelivery(notification, channel, DeliveryStatus.Skipped);
        }

        var results = await Task.WhenAll(pending.Select(p => p.Send));
        for (var i = 0; i < pending.Count; i++)
        {
            pending[i].Delivery.Status = results[i] ? DeliveryStatus.Sent : DeliveryStatus.Failed;
            pending[i].Delivery.AttemptedAt = DateTimeOffset.UtcNow;
        }

        notification.Status = Array.TrueForAll(results, r => r)
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

    private async Task<bool> SendAsync(
        NotificationChannel channel,
        NotificationEntity notification,
        NotificationPreferenceEntity? prefs,
        CancellationToken cancellationToken)
    {
        var push = new NotificationPushDto(notification.Id, notification.Source, notification.Type, notification.Subject, notification.Body, notification.Priority, notification.CreatedAt);

        if (channel == NotificationChannel.InApp)
        {
            return await hubPushClient.PushAsync(notification.UserId, push, cancellationToken);
        }

        var destination = channel == NotificationChannel.Email ? prefs?.Email : prefs?.PhoneNumber;
        if (destination is null || !_senders.TryGetValue(channel, out var sender))
        {
            return false;
        }

        try
        {
            return (await sender.SendAsync(push, destination, cancellationToken)).Success;
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    /// <summary>
    /// InApp is always attempted — it's free and the hub degrades to Skipped when the user has no
    /// active connection. Email needs the per-type toggle (default on) and an address. SMS is
    /// per-type opt-in (default off), except High priority, which attempts SMS unless the user
    /// set <c>SmsOptOut</c>. Channels without a registered sender are never resolved.
    /// </summary>
    private HashSet<NotificationChannel> ResolveChannels(
        NotificationEntity notification,
        NotificationPreferenceEntity? prefs,
        NotificationTypePreferenceEntity? typePref)
    {
        var channels = new HashSet<NotificationChannel> { NotificationChannel.InApp };

        var emailEnabled = typePref?.EmailEnabled ?? true;
        if (emailEnabled && !string.IsNullOrEmpty(prefs?.Email) && _senders.ContainsKey(NotificationChannel.Email))
        {
            channels.Add(NotificationChannel.Email);
        }

        var smsAllowed = notification.Priority == NotificationPriority.High
            ? !(prefs?.SmsOptOut ?? false)
            : typePref?.SmsEnabled ?? false;
        if (smsAllowed && !string.IsNullOrEmpty(prefs?.PhoneNumber) && _senders.ContainsKey(NotificationChannel.Sms))
        {
            channels.Add(NotificationChannel.Sms);
        }

        return channels;
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spydersoft.Notification.Contracts;
using Spydersoft.NotificationApi.Dispatch;
using Spydersoft.NotificationApi.Infrastructure.Data;
using Spydersoft.NotificationApi.Infrastructure.Data.Entities;

namespace Spydersoft.NotificationApi.Controllers;

[ApiController]
[Route("api/v1/notifications")]
[Tags("Notifications")]
public class NotificationsController(NotificationDbContext db, NotificationDispatchQueue dispatchQueue) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.Write)]
    [ProducesResponseType(typeof(NotificationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateNotificationRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserId) || string.IsNullOrWhiteSpace(request.Source) ||
            string.IsNullOrWhiteSpace(request.Type) || string.IsNullOrWhiteSpace(request.Subject) ||
            string.IsNullOrWhiteSpace(request.Body))
        {
            return Problem(detail: "userId, source, type, subject, and body are required.", statusCode: StatusCodes.Status400BadRequest);
        }

        var notification = new NotificationEntity
        {
            UserId = request.UserId,
            Source = request.Source,
            Type = request.Type,
            Subject = request.Subject,
            Body = request.Body,
            Data = request.Data,
            Priority = request.Priority,
            EntityType = request.EntityType,
            EntityId = request.EntityId,
        };

        db.Notifications.Add(notification);
        await db.SaveChangesAsync(cancellationToken);

        await dispatchQueue.EnqueueAsync(new DispatchItem(notification.Id), cancellationToken);

        return Created($"/api/v1/notifications/{notification.Id}", ToDto(notification));
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.Read)]
    [ProducesResponseType(typeof(IReadOnlyList<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] bool unreadOnly,
        [FromQuery] string? source,
        [FromQuery] string? type,
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 200);
        skip = Math.Max(skip, 0);

        var userId = GetUserId();
        var query = db.Notifications.Where(n => n.UserId == userId);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        if (!string.IsNullOrEmpty(source))
        {
            query = query.Where(n => n.Source == source);
        }

        if (!string.IsNullOrEmpty(type))
        {
            query = query.Where(n => n.Type == type);
        }

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip(skip)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return Ok(notifications.Select(n => ToDto(n)));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.Read)]
    [ProducesResponseType(typeof(NotificationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var notification = await db.Notifications
            .Include(n => n.Deliveries)
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId, cancellationToken);

        return notification is null ? NotFound() : Ok(ToDto(notification, includeDeliveries: true));
    }

    [HttpPost("{id:guid}/read")]
    [Authorize(Policy = AuthorizationPolicies.Write)]
    [ProducesResponseType(typeof(NotificationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var notification = await db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId, cancellationToken);
        if (notification is null)
        {
            return NotFound();
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }

        return Ok(ToDto(notification));
    }

    [HttpPost("read-all")]
    [Authorize(Policy = AuthorizationPolicies.Write)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var unread = await db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);

        var readAt = DateTimeOffset.UtcNow;
        foreach (var notification in unread)
        {
            notification.IsRead = true;
            notification.ReadAt = readAt;
        }

        await db.SaveChangesAsync(cancellationToken);

        return Ok(new { updatedCount = unread.Count });
    }

    [HttpGet("unread-count")]
    [Authorize(Policy = AuthorizationPolicies.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UnreadCount(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var count = await db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
        return Ok(new { count });
    }

    private static NotificationDto ToDto(NotificationEntity n, bool includeDeliveries = false) =>
        new(
            n.Id, n.UserId, n.Source, n.Type, n.Subject, n.Body, n.Data,
            n.Priority, n.Status, n.IsRead, n.ReadAt, n.CreatedAt, n.EntityType, n.EntityId,
            includeDeliveries
                ? n.Deliveries.Select(d => new NotificationDeliveryDto(d.Channel, d.Status, d.ExternalId, d.Error, d.AttemptedAt)).ToList()
                : null);

    private string GetUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")?.Value
        ?? string.Empty;
}

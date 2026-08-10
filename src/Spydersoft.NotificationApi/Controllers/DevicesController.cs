using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spydersoft.Notification.Contracts;
using Spydersoft.NotificationApi.Infrastructure.Data;
using Spydersoft.NotificationApi.Infrastructure.Data.Entities;

namespace Spydersoft.NotificationApi.Controllers;

[ApiController]
[Route("api/v1/devices")]
[Tags("Devices")]
public class DevicesController(NotificationDbContext db) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.Write)]
    [ProducesResponseType(typeof(DeviceDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Register(RegisterDeviceRequest request, CancellationToken cancellationToken)
    {
        var device = new DeviceEntity
        {
            UserId = GetUserId(),
            DeviceType = request.DeviceType,
            Label = request.Label,
            PushToken = request.PushToken,
        };

        db.Devices.Add(device);
        await db.SaveChangesAsync(cancellationToken);

        return Created($"/api/v1/devices/{device.Id}", ToDto(device));
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.Read)]
    [ProducesResponseType(typeof(IReadOnlyList<DeviceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] bool includeInactive, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var query = db.Devices.Where(d => d.UserId == userId);
        if (!includeInactive)
        {
            query = query.Where(d => d.IsActive);
        }

        var devices = await query.ToListAsync(cancellationToken);
        return Ok(devices.Select(ToDto));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deregister(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var device = await db.Devices.FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId, cancellationToken);
        if (device is null)
        {
            return NotFound();
        }

        device.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static DeviceDto ToDto(DeviceEntity d) =>
        new(d.Id, d.DeviceType, d.Label, d.LastSeenAt, d.RegisteredAt, d.IsActive);

    private string GetUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")?.Value
        ?? string.Empty;
}

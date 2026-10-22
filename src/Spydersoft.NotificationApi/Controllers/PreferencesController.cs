using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spydersoft.Notification.Contracts;
using Spydersoft.NotificationApi.Infrastructure.Data;
using Spydersoft.NotificationApi.Infrastructure.Data.Entities;

namespace Spydersoft.NotificationApi.Controllers;

[ApiController]
[Route("api/v1/preferences")]
[Tags("Preferences")]
public partial class PreferencesController(NotificationDbContext db) : ControllerBase
{
    private static readonly Regex EmailPattern = EmailRegex();
    private static readonly Regex PhonePattern = PhoneRegex();

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.Read)]
    [ProducesResponseType(typeof(NotificationPreferenceDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        var preference = await db.NotificationPreferences.FindAsync([userId], cancellationToken);
        var typePreferences = await db.NotificationTypePreferences
            .Where(p => p.UserId == userId)
            .ToListAsync(cancellationToken);

        return Ok(ToDto(preference, typePreferences));
    }

    [HttpPut]
    [Authorize(Policy = AuthorizationPolicies.Write)]
    [ProducesResponseType(typeof(NotificationPreferenceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(UpdatePreferencesRequest request, CancellationToken cancellationToken)
    {
        if (request.Email is not null && !EmailPattern.IsMatch(request.Email))
        {
            return Problem(detail: "email is not a valid address.", statusCode: StatusCodes.Status400BadRequest);
        }

        if (request.PhoneNumber is not null && !PhonePattern.IsMatch(request.PhoneNumber))
        {
            return Problem(detail: "phoneNumber must be in E.164 format.", statusCode: StatusCodes.Status400BadRequest);
        }

        var userId = GetUserId();
        var preference = await db.NotificationPreferences.FindAsync([userId], cancellationToken);
        if (preference is null)
        {
            preference = new NotificationPreferenceEntity { UserId = userId };
            db.NotificationPreferences.Add(preference);
        }

        preference.Email = request.Email;
        preference.PhoneNumber = request.PhoneNumber;
        preference.SmsOptOut = request.SmsOptOut;
        preference.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        var typePreferences = await db.NotificationTypePreferences
            .Where(p => p.UserId == userId)
            .ToListAsync(cancellationToken);

        return Ok(ToDto(preference, typePreferences));
    }

    [HttpPut("types/{source}/{type}")]
    [Authorize(Policy = AuthorizationPolicies.Write)]
    [ProducesResponseType(typeof(NotificationTypePreferenceDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateType(string source, string type, UpdateTypePreferenceRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var typePreference = await db.NotificationTypePreferences
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Source == source && p.Type == type, cancellationToken);

        if (typePreference is null)
        {
            typePreference = new NotificationTypePreferenceEntity { UserId = userId, Source = source, Type = type };
            db.NotificationTypePreferences.Add(typePreference);
        }

        typePreference.EmailEnabled = request.EmailEnabled;
        typePreference.SmsEnabled = request.SmsEnabled;

        await db.SaveChangesAsync(cancellationToken);

        return Ok(ToTypeDto(typePreference));
    }

    [HttpDelete("types/{source}/{type}")]
    [Authorize(Policy = AuthorizationPolicies.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResetType(string source, string type, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var typePreference = await db.NotificationTypePreferences
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Source == source && p.Type == type, cancellationToken);

        if (typePreference is not null)
        {
            db.NotificationTypePreferences.Remove(typePreference);
            await db.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    private static NotificationPreferenceDto ToDto(NotificationPreferenceEntity? preference, List<NotificationTypePreferenceEntity> typePreferences) =>
        new(
            preference?.Email,
            preference?.PhoneNumber,
            preference?.SmsOptOut ?? false,
            typePreferences.Select(ToTypeDto).ToList());

    private static NotificationTypePreferenceDto ToTypeDto(NotificationTypePreferenceEntity p) =>
        new(p.Source, p.Type, p.EmailEnabled, p.SmsEnabled);

    private string GetUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")?.Value
        ?? string.Empty;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"^\+[1-9]\d{1,14}$")]
    private static partial Regex PhoneRegex();
}

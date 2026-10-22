using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spydersoft.Notification.Contracts;
using Spydersoft.NotificationApi.Controllers;
using Spydersoft.NotificationApi.Infrastructure.Data;

namespace Spydersoft.NotificationApi.UnitTests.Controllers;

[TestFixture]
public sealed class PreferencesControllerTests
{
    [Test]
    public async Task Get_NoRowsYet_ReturnsDefaults()
    {
        using var db = CreateDb();
        var controller = CreateController(db, "user-a");

        var result = (OkObjectResult)await controller.Get(CancellationToken.None);
        var dto = (NotificationPreferenceDto)result.Value!;

        Assert.That(dto.Email, Is.Null);
        Assert.That(dto.PhoneNumber, Is.Null);
        Assert.That(dto.SmsOptOut, Is.False);
        Assert.That(dto.TypePreferences, Is.Empty);
    }

    [Test]
    public async Task Update_ValidEmailAndPhone_Persists()
    {
        using var db = CreateDb();
        var controller = CreateController(db, "user-a");

        var result = (OkObjectResult)await controller.Update(
            new UpdatePreferencesRequest("user@example.com", "+15551234567", true), CancellationToken.None);
        var dto = (NotificationPreferenceDto)result.Value!;

        Assert.That(dto.Email, Is.EqualTo("user@example.com"));
        Assert.That(dto.PhoneNumber, Is.EqualTo("+15551234567"));
        Assert.That(dto.SmsOptOut, Is.True);

        var reGet = (OkObjectResult)await controller.Get(CancellationToken.None);
        var reGetDto = (NotificationPreferenceDto)reGet.Value!;
        Assert.That(reGetDto.Email, Is.EqualTo("user@example.com"));
    }

    [Test]
    public async Task Update_SecondCall_UpdatesSameRow_NoDuplicate()
    {
        using var db = CreateDb();
        var controller = CreateController(db, "user-a");

        await controller.Update(new UpdatePreferencesRequest("first@example.com", null, false), CancellationToken.None);
        await controller.Update(new UpdatePreferencesRequest("second@example.com", null, true), CancellationToken.None);

        Assert.That(db.NotificationPreferences.Count(p => p.UserId == "user-a"), Is.EqualTo(1));
        var stored = db.NotificationPreferences.Single(p => p.UserId == "user-a");
        Assert.That(stored.Email, Is.EqualTo("second@example.com"));
        Assert.That(stored.SmsOptOut, Is.True);
    }

    [Test]
    public async Task Update_InvalidEmail_ReturnsBadRequest()
    {
        using var db = CreateDb();
        var controller = CreateController(db, "user-a");

        var result = await controller.Update(new UpdatePreferencesRequest("not-an-email", null, false), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
    }

    [Test]
    public async Task Update_InvalidPhoneFormat_ReturnsBadRequest()
    {
        using var db = CreateDb();
        var controller = CreateController(db, "user-a");

        var result = await controller.Update(new UpdatePreferencesRequest(null, "555-1234", false), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
    }

    [Test]
    public async Task UpdateType_FirstCall_CreatesRow_SecondCall_UpdatesIt()
    {
        using var db = CreateDb();
        var controller = CreateController(db, "user-a");

        var first = (OkObjectResult)await controller.UpdateType(
            "pitstop", "recall-alert", new UpdateTypePreferenceRequest(true, true), CancellationToken.None);
        var firstDto = (NotificationTypePreferenceDto)first.Value!;
        Assert.That(firstDto.SmsEnabled, Is.True);
        Assert.That(db.NotificationTypePreferences.Count(), Is.EqualTo(1));

        var second = (OkObjectResult)await controller.UpdateType(
            "pitstop", "recall-alert", new UpdateTypePreferenceRequest(false, false), CancellationToken.None);
        var secondDto = (NotificationTypePreferenceDto)second.Value!;
        Assert.That(secondDto.EmailEnabled, Is.False);
        Assert.That(db.NotificationTypePreferences.Count(), Is.EqualTo(1), "second PUT should update, not duplicate, the row");
    }

    [Test]
    public async Task ResetType_RemovesOverride_GetReflectsDefaultsAgain()
    {
        using var db = CreateDb();
        var controller = CreateController(db, "user-a");

        await controller.UpdateType("pitstop", "recall-alert", new UpdateTypePreferenceRequest(false, true), CancellationToken.None);
        Assert.That((await controller.Get(CancellationToken.None) as OkObjectResult)!.Value is NotificationPreferenceDto { TypePreferences.Count: 1 });

        var resetResult = await controller.ResetType("pitstop", "recall-alert", CancellationToken.None);
        Assert.That(resetResult, Is.InstanceOf<NoContentResult>());

        var afterReset = (OkObjectResult)await controller.Get(CancellationToken.None);
        Assert.That(((NotificationPreferenceDto)afterReset.Value!).TypePreferences, Is.Empty);
    }

    [Test]
    public async Task Preferences_AreScopedPerUser()
    {
        using var db = CreateDb();
        var userA = CreateController(db, "user-a");
        var userB = CreateController(db, "user-b");

        await userA.Update(new UpdatePreferencesRequest("a@example.com", null, false), CancellationToken.None);

        var bResult = (OkObjectResult)await userB.Get(CancellationToken.None);
        Assert.That(((NotificationPreferenceDto)bResult.Value!).Email, Is.Null);
    }

    private static NotificationDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<NotificationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static PreferencesController CreateController(NotificationDbContext db, string userId)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId)], "test"));
        return new PreferencesController(db)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = principal } },
        };
    }
}

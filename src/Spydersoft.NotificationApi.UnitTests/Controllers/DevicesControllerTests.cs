using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spydersoft.Notification.Contracts;
using Spydersoft.NotificationApi.Controllers;
using Spydersoft.NotificationApi.Infrastructure.Data;

namespace Spydersoft.NotificationApi.UnitTests.Controllers;

[TestFixture]
public sealed class DevicesControllerTests
{
    [Test]
    public async Task Register_ThenList_ThenDeregister_Lifecycle()
    {
        using var db = CreateDb();
        var controller = CreateController(db, "user-a");

        var registered = (CreatedResult)await controller.Register(new RegisterDeviceRequest(DeviceType.Web, "Chrome on MacBook"), CancellationToken.None);
        var device = (DeviceDto)registered.Value!;

        var listed = (OkObjectResult)await controller.List(includeInactive: false, CancellationToken.None);
        Assert.That(((IEnumerable<DeviceDto>)listed.Value!).Select(d => d.Id), Does.Contain(device.Id));

        var deregistered = await controller.Deregister(device.Id, CancellationToken.None);
        Assert.That(deregistered, Is.InstanceOf<NoContentResult>());

        var afterDeregister = (OkObjectResult)await controller.List(includeInactive: false, CancellationToken.None);
        Assert.That(((IEnumerable<DeviceDto>)afterDeregister.Value!).Select(d => d.Id), Does.Not.Contain(device.Id));

        var withInactive = (OkObjectResult)await controller.List(includeInactive: true, CancellationToken.None);
        Assert.That(((IEnumerable<DeviceDto>)withInactive.Value!).Select(d => d.Id), Does.Contain(device.Id));
    }

    [Test]
    public async Task Deregister_OtherUsersDevice_ReturnsNotFound()
    {
        using var db = CreateDb();
        var ownerController = CreateController(db, "user-a");
        var registered = (CreatedResult)await ownerController.Register(new RegisterDeviceRequest(DeviceType.Web, "Chrome"), CancellationToken.None);
        var device = (DeviceDto)registered.Value!;

        var attackerController = CreateController(db, "user-b");
        var result = await attackerController.Deregister(device.Id, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    private static NotificationDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<NotificationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static DevicesController CreateController(NotificationDbContext db, string userId)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId)], "test"));
        return new DevicesController(db)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = principal } },
        };
    }
}

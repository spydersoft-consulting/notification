using Microsoft.EntityFrameworkCore;
using Spydersoft.NotificationApi.Infrastructure.Data;

namespace Spydersoft.NotificationApi.Endpoints;

internal static class TestEndpoints
{
    public static IEndpointRouteBuilder MapTestEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/test").WithTags("Test");

        group.MapDelete("/notifications", async (
            string userId,
            NotificationDbContext db,
            CancellationToken ct) =>
        {
            var notificationIds = db.Notifications.Where(n => n.UserId == userId).Select(n => n.Id);
            await db.NotificationDeliveries.Where(d => notificationIds.Contains(d.NotificationId)).ExecuteDeleteAsync(ct);
            await db.Notifications.Where(n => n.UserId == userId).ExecuteDeleteAsync(ct);
            await db.Devices.Where(d => d.UserId == userId).ExecuteDeleteAsync(ct);

            return Results.NoContent();
        })
        .Produces(StatusCodes.Status204NoContent);

        return app;
    }
}

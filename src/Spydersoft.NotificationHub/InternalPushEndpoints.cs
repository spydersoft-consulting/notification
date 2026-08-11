using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Spydersoft.Notification.Contracts;

namespace Spydersoft.NotificationHub;

internal static class InternalPushEndpoints
{
    public static IEndpointRouteBuilder MapInternalPushEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/internal/push", async (
            HttpRequest httpRequest,
            IHubContext<NotificationHub> hubContext,
            IOptions<InternalPushOptions> options,
            CancellationToken ct) =>
        {
            if (!IsAuthorized(httpRequest, options.Value.HubInternalToken))
            {
                return Results.Unauthorized();
            }

            var request = await httpRequest.ReadFromJsonAsync<HubPushRequest>(ct);
            if (request is null)
            {
                return Results.BadRequest();
            }

            await hubContext.Clients.Group(NotificationHub.GroupName(request.UserId))
                .SendAsync("ReceiveNotification", request.Notification, ct);

            return Results.Accepted();
        });

        return app;
    }

    private static bool IsAuthorized(HttpRequest request, string? expectedToken)
    {
        if (string.IsNullOrEmpty(expectedToken))
        {
            return false;
        }

        var header = request.Headers.Authorization.ToString();
        const string prefix = "Bearer ";
        if (!header.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var presented = header[prefix.Length..];
        return CryptographicOperations.FixedTimeEquals(presented, expectedToken);
    }
}

file static class CryptographicOperations
{
    public static bool FixedTimeEquals(string a, string b) =>
        System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(a), System.Text.Encoding.UTF8.GetBytes(b));
}

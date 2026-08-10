# Spydersoft Notification

Platform notification service for Spydersoft applications. Accepts a notification request from
any Spydersoft app over HTTP, persists it, tracks read/unread state per user, and fans it out to
whichever channels the user's preferences and device registrations call for — in-app (SignalR),
email, and/or SMS.

**v1 ships with the `InApp` (SignalR) channel only.** See
[../plans/notifications/overview.md](../plans/notifications/overview.md) for the full design.

## Projects

- `Spydersoft.Notification.Contracts` — Wire DTOs, enums, and client interfaces (NuGet)
- `Spydersoft.Notification.Client` — HTTP client implementations + SignalR connection helper (NuGet)
- `Spydersoft.NotificationApi` — ASP.NET Core 10 API: core storage, devices, in-process dispatcher, router
- `Spydersoft.NotificationHub` — ASP.NET Core 10 SignalR host for real-time in-app push
- `Spydersoft.Notification.TokenGenerator` — Console app that mints test JWTs for local/e2e use
- `Spydersoft.Notification.AppHost` — .NET Aspire local development host

## Local Development

```powershell
dotnet run --project src/Spydersoft.Notification.AppHost
```

Requires Docker. Starts PostgreSQL, the API, and the hub.

## Consuming the Client

Register `Spydersoft.Notification.Client` in a consuming app's DI container:

```csharp
builder.Services.AddSpydersoftNotification(builder.Configuration);
```

This registers `INotificationClient` and `IDeviceClient` as typed `HttpClient`s, bound to a
`Notification` configuration section:

```json
{
  "Notification": {
    "BaseUrl": "https://notify.example.com",
    "HubUrl": "https://notify-hub.example.com/hubs/notifications"
  }
}
```

## Codebase Conventions

- All C# classes sealed unless explicitly designed for inheritance
- Nullable reference types enabled globally
- Implicit usings enabled
- NUnit + NSubstitute for testing
- Spydersoft.Platform.Hosting for telemetry and health checks

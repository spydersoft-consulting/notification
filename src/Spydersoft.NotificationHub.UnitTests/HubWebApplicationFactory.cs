using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Spydersoft.NotificationHub.UnitTests;

internal sealed class HubWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string InternalPushToken = "test-internal-push-token";

    public HubWebApplicationFactory()
    {
        // Program.cs reads configuration (including Telemetry:*) while executing top-level
        // statements, before WebApplicationFactory's ConfigureWebHost hooks are wired up — so
        // environment variables (present from process start) are the reliable way to override
        // it for this host, unlike ConfigureAppConfiguration.
        Environment.SetEnvironmentVariable("Auth__TestKey", TestJwt.SigningKeyBase64);
        Environment.SetEnvironmentVariable("Notification__HubInternalToken", InternalPushToken);
        Environment.SetEnvironmentVariable("Telemetry__Enabled", "false");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Testing");
}

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres").WithPgAdmin();
var notificationDb = postgres.AddDatabase("notification");

// Aspire dashboard OTLP endpoint
var dashboardOtlp = builder.Configuration["DOTNET_DASHBOARD_OTLP_ENDPOINT_URL"]
    ?? "http://localhost:18889";

var hub = builder.AddProject<Projects.Spydersoft_NotificationHub>("notification-hub")
    .WithEnvironment("Notification__HubInternalToken", "local-dev-token");

var api = builder.AddProject<Projects.Spydersoft_NotificationApi>("notification-api")
    .WithReference(notificationDb)
    .WithEnvironment("Notification__HubInternalUrl", $"{hub.GetEndpoint("http")}/internal")
    .WithEnvironment("Notification__HubInternalToken", "local-dev-token")
    .WaitFor(postgres)
    .WaitFor(hub);

// Telemetry env vars (same pattern as the other Spydersoft platform services)
foreach (var (typeKey, endpointKey) in new[]
{
    ("Telemetry__Trace__Type",   "Telemetry__Trace__Otlp__Endpoint"),
    ("Telemetry__Metrics__Type", "Telemetry__Metrics__Otlp__Endpoint"),
    ("Telemetry__Log__Type",     "Telemetry__Log__Otlp__Endpoint"),
})
{
    foreach (var resource in new[] { api, hub })
    {
        resource.WithEnvironment(typeKey, builder.Configuration[typeKey] ?? "otlp");
        resource.WithEnvironment(endpointKey, builder.Configuration[endpointKey] ?? dashboardOtlp);
    }
}

if (builder.Environment.EnvironmentName == "Testing")
{
    var testKey = builder.Configuration["Auth:TestKey"]
        ?? "jRv3YFPH/19t9t5CgsEFgAkykfW5bQhHmceMprLgzlQ=";

    api.WithEnvironment("DOTNET_ENVIRONMENT", "Testing")
       .WithEnvironment("Auth__TestKey", testKey)
       .WithEndpoint("http", e => e.Port = 5300);

    hub.WithEnvironment("DOTNET_ENVIRONMENT", "Testing")
       .WithEnvironment("Auth__TestKey", testKey)
       .WithEndpoint("http", e => e.Port = 5301);
}
else
{
    foreach (var resource in new[] { api, hub })
    {
        resource.WithEnvironment("Auth__Authority", builder.Configuration["Auth:Authority"] ?? "https://auth.mattgerega.net")
                .WithEnvironment("Auth__Audience", builder.Configuration["Auth:Audience"] ?? "notification-api");
    }
}

await builder.Build().RunAsync();

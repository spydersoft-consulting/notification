using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Spydersoft.NotificationApi;
using Spydersoft.NotificationApi.Dispatch;
using Spydersoft.NotificationApi.Endpoints;
using Spydersoft.NotificationApi.Hub;
using Spydersoft.NotificationApi.Infrastructure.Data;
using Spydersoft.NotificationApi.Routing;
using Spydersoft.Platform.Hosting.StartupExtensions;
using Spydersoft.Platform.Hosting.Telemetry;

var builder = WebApplication.CreateBuilder(args);

builder.AddSpydersoftTelemetry(typeof(Program).Assembly,
    new ConfigurationFunctions
    {
        // Kubernetes probes hit these every few seconds; they add nothing but noise to traces.
        AspNetFilterFunction = context => !IsHealthCheckPath(context.Request.Path.Value)
    })
       .AddSpydersoftSerilog();

var healthCheckOptions = builder.AddSpydersoftHealthChecks();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        if (builder.Environment.IsEnvironment("Testing"))
        {
            var testKey = builder.Configuration["Auth:TestKey"]!;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false,
                IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(testKey)),
            };
        }
        else
        {
            options.Authority = builder.Configuration["Auth:Authority"];
            options.Audience = builder.Configuration["Auth:Audience"];
        }
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AuthorizationPolicies.Read, p => p.RequireClaim("scope", AuthorizationPolicies.Read))
    .AddPolicy(AuthorizationPolicies.Write, p => p.RequireClaim("scope", AuthorizationPolicies.Write));

builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("notification"))
           .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

builder.Services.Configure<HubOptions>(builder.Configuration.GetSection(HubOptions.SectionName));
builder.Services.AddHttpClient<IHubPushClient, HubPushClient>();

builder.Services.AddSingleton<NotificationDispatchQueue>();
builder.Services.AddHostedService<NotificationDispatcherService>();
builder.Services.AddHostedService<DispatchReconciliationService>();
builder.Services.AddScoped<INotificationRouter, NotificationRouter>();

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddNotificationOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseSpydersoftRequestLogging();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (app.Environment.IsEnvironment("Testing"))
{
    app.MapTestEndpoints();
}

app.UseSpydersoftHealthChecks(healthCheckOptions);

await app.RunAsync();

static bool IsHealthCheckPath(string? path) =>
    string.Equals(path, "/livez", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(path, "/readyz", StringComparison.OrdinalIgnoreCase);

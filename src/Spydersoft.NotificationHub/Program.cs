using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Spydersoft.NotificationHub;
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

const string HubPath = "/hubs/notifications";

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

        // Browsers can't set headers on the WebSocket upgrade request, so SignalR's documented
        // pattern is to accept the token via query string for hub paths only.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments(HubPath))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            },
        };
    });

builder.Services.AddAuthorization();

builder.Services.Configure<InternalPushOptions>(builder.Configuration.GetSection(InternalPushOptions.SectionName));

builder.Services.AddSignalR();

builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

app.UseSpydersoftRequestLogging();
app.UseAuthentication();
app.UseAuthorization();

app.MapHub<NotificationHub>(HubPath);
app.MapInternalPushEndpoints();

app.UseSpydersoftHealthChecks(healthCheckOptions);

await app.RunAsync();

static bool IsHealthCheckPath(string? path) =>
    string.Equals(path, "/livez", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(path, "/readyz", StringComparison.OrdinalIgnoreCase);

public partial class Program;

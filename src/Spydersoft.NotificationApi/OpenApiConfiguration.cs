using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Spydersoft.NotificationApi;

internal static class OpenApiConfiguration
{
    private const string BearerSchemeName = "bearerAuth";

    public static IServiceCollection AddNotificationOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "Spydersoft Notification API",
                    Version = "v1",
                    Description = "Platform notification service: core storage, device registry, and in-process dispatch.",
                };
                document.Servers = [];
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes[BearerSchemeName] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "JWT bearer token with notification:read or notification:write scope.",
                };
                document.Security =
                [
                    new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference(BearerSchemeName, document)] = [],
                    },
                ];
                return Task.CompletedTask;
            });
        });
        return services;
    }
}

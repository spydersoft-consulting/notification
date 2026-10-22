using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Spydersoft.Notification.Contracts;

namespace Spydersoft.Notification.Client;

public static class NotificationServiceCollectionExtensions
{
    public static IServiceCollection AddSpydersoftNotification(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<NotificationOptions>()
            .Bind(configuration.GetSection(NotificationOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.BaseUrl), "NotificationOptions.BaseUrl is required.")
            .ValidateOnStart();

        void Configure(IServiceProvider sp, HttpClient http)
        {
            var opts = sp.GetRequiredService<IOptions<NotificationOptions>>().Value;
            http.BaseAddress = new Uri(opts.BaseUrl.TrimEnd('/'));
        }

        services.AddHttpClient<INotificationClient, NotificationHttpClient>(Configure);
        services.AddHttpClient<IDeviceClient, DeviceHttpClient>(Configure);
        services.AddHttpClient<IPreferencesClient, PreferencesHttpClient>(Configure);
        services.AddSingleton<NotificationHubConnectionFactory>();

        return services;
    }
}

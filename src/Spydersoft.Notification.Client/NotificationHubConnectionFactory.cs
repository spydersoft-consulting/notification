using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;

namespace Spydersoft.Notification.Client;

/// <summary>
/// Thin convenience wrapper for a .NET SignalR client (e.g. a future desktop/MAUI app).
/// Web frontends should use the JS SignalR client directly against the hub URL.
/// </summary>
public sealed class NotificationHubConnectionFactory
{
    private readonly NotificationOptions _options;

    public NotificationHubConnectionFactory(IOptions<NotificationOptions> options)
    {
        _options = options.Value;
    }

    public HubConnection Create(string accessToken)
    {
        if (string.IsNullOrWhiteSpace(_options.HubUrl))
        {
            throw new InvalidOperationException("Notification:HubUrl is not configured.");
        }

        return new HubConnectionBuilder()
            .WithUrl(_options.HubUrl, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
            })
            .WithAutomaticReconnect()
            .Build();
    }
}

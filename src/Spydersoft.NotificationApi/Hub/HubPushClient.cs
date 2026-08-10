using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Spydersoft.Notification.Contracts;

namespace Spydersoft.NotificationApi.Hub;

public sealed class HubPushClient : IHubPushClient
{
    private readonly HttpClient _http;
    private readonly ILogger<HubPushClient> _logger;
    private readonly HubOptions _options;

    public HubPushClient(HttpClient http, IOptions<HubOptions> options, ILogger<HubPushClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> PushAsync(string userId, NotificationPushDto notification, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.HubInternalUrl))
        {
            _logger.LogWarning("Notification:HubInternalUrl is not configured; skipping InApp push for {NotificationId}", notification.Id);
            return false;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.HubInternalUrl.TrimEnd('/')}/push")
            {
                Content = JsonContent.Create(new HubPushRequest(userId, notification)),
            };
            if (!string.IsNullOrEmpty(_options.HubInternalToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.HubInternalToken);
            }

            var response = await _http.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Hub push failed for notification {NotificationId}", notification.Id);
            return false;
        }
    }
}

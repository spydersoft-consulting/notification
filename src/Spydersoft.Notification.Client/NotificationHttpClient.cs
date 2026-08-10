using System.Net.Http.Json;
using Spydersoft.Notification.Contracts;

namespace Spydersoft.Notification.Client;

public sealed class NotificationHttpClient : INotificationClient
{
    private readonly HttpClient _http;

    public NotificationHttpClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<NotificationDto> CreateAsync(CreateNotificationRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync("/api/v1/notifications", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<NotificationDto>(cancellationToken))!;
    }

    public async Task<IReadOnlyList<NotificationDto>> ListAsync(
        bool unreadOnly = false,
        string? source = null,
        string? type = null,
        int skip = 0,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var query = HttpClientHelpers.BuildQuery([
            ("unreadOnly", unreadOnly ? "true" : null),
            ("source", source),
            ("type", type),
            ("skip", skip == 0 ? null : skip.ToString()),
            ("limit", limit == 50 ? null : limit.ToString()),
        ]);
        var result = await _http.GetFromJsonAsync<List<NotificationDto>>($"/api/v1/notifications{query}", cancellationToken);
        return result ?? [];
    }

    public async Task<NotificationDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _http.GetFromJsonAsync<NotificationDto>($"/api/v1/notifications/{id}", cancellationToken);
        return response!;
    }

    public async Task<NotificationDto> MarkReadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsync($"/api/v1/notifications/{id}/read", null, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<NotificationDto>(cancellationToken))!;
    }

    public async Task<int> MarkAllReadAsync(CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsync("/api/v1/notifications/read-all", null, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<MarkAllReadResponse>(cancellationToken);
        return result?.UpdatedCount ?? 0;
    }

    public async Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default)
    {
        var result = await _http.GetFromJsonAsync<UnreadCountResponse>("/api/v1/notifications/unread-count", cancellationToken);
        return result?.Count ?? 0;
    }

    private sealed record MarkAllReadResponse(int UpdatedCount);

    private sealed record UnreadCountResponse(int Count);
}

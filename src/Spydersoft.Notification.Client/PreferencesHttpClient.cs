using System.Net.Http.Json;
using Spydersoft.Notification.Contracts;

namespace Spydersoft.Notification.Client;

public sealed class PreferencesHttpClient : IPreferencesClient
{
    private readonly HttpClient _http;

    public PreferencesHttpClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<NotificationPreferenceDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var result = await _http.GetFromJsonAsync<NotificationPreferenceDto>("/api/v1/preferences", cancellationToken);
        return result!;
    }

    public async Task<NotificationPreferenceDto> UpdateAsync(UpdatePreferencesRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _http.PutAsJsonAsync("/api/v1/preferences", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<NotificationPreferenceDto>(cancellationToken))!;
    }

    public async Task<NotificationTypePreferenceDto> UpdateTypeAsync(
        string source, string type, UpdateTypePreferenceRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _http.PutAsJsonAsync(
            $"/api/v1/preferences/types/{Uri.EscapeDataString(source)}/{Uri.EscapeDataString(type)}", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<NotificationTypePreferenceDto>(cancellationToken))!;
    }

    public async Task ResetTypeAsync(string source, string type, CancellationToken cancellationToken = default)
    {
        var response = await _http.DeleteAsync(
            $"/api/v1/preferences/types/{Uri.EscapeDataString(source)}/{Uri.EscapeDataString(type)}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}

using System.Net.Http.Json;
using Spydersoft.Notification.Contracts;

namespace Spydersoft.Notification.Client;

public sealed class DeviceHttpClient : IDeviceClient
{
    private readonly HttpClient _http;

    public DeviceHttpClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<DeviceDto> RegisterAsync(RegisterDeviceRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync("/api/v1/devices", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DeviceDto>(cancellationToken))!;
    }

    public async Task<IReadOnlyList<DeviceDto>> ListAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = HttpClientHelpers.BuildQuery([("includeInactive", includeInactive ? "true" : null)]);
        var result = await _http.GetFromJsonAsync<List<DeviceDto>>($"/api/v1/devices{query}", cancellationToken);
        return result ?? [];
    }

    public async Task DeregisterAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _http.DeleteAsync($"/api/v1/devices/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}

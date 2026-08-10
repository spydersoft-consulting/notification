using System.Net;
using System.Net.Http.Json;
using Spydersoft.Notification.Contracts;

namespace Spydersoft.Notification.Client.UnitTests;

[TestFixture]
public sealed class DeviceHttpClientTests
{
    [Test]
    public async Task RegisterAsync_ReturnsDto_WhenSuccessful()
    {
        var expected = new DeviceDto(Guid.NewGuid(), DeviceType.Web, "Chrome on MacBook", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, true);
        var handler = new MockHttpMessageHandler(HttpStatusCode.Created, JsonContent.Create(expected));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var client = new DeviceHttpClient(httpClient);

        var request = new RegisterDeviceRequest(DeviceType.Web, "Chrome on MacBook");
        var result = await client.RegisterAsync(request);

        Assert.That(result.Id, Is.EqualTo(expected.Id));
        Assert.That(handler.LastRequest?.RequestUri?.AbsolutePath, Is.EqualTo("/api/v1/devices"));
    }

    [Test]
    public async Task DeregisterAsync_SendsDelete()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.NoContent);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var client = new DeviceHttpClient(httpClient);

        var id = Guid.NewGuid();
        Assert.DoesNotThrowAsync(() => client.DeregisterAsync(id));
    }

    [Test]
    public async Task ListAsync_IncludeInactive_AddsQueryParam()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, JsonContent.Create(new List<DeviceDto>()));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var client = new DeviceHttpClient(httpClient);

        await client.ListAsync(includeInactive: true);

        Assert.That(handler.LastRequest?.RequestUri?.Query, Does.Contain("includeInactive=true"));
    }
}

using System.Net;
using System.Net.Http.Json;
using Spydersoft.Notification.Contracts;

namespace Spydersoft.Notification.Client.UnitTests;

[TestFixture]
public sealed class PreferencesHttpClientTests
{
    [Test]
    public async Task GetAsync_ReturnsDto()
    {
        var expected = new NotificationPreferenceDto("user@example.com", "+15551234567", false, []);
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, JsonContent.Create(expected));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var client = new PreferencesHttpClient(httpClient);

        var result = await client.GetAsync();

        Assert.That(result.Email, Is.EqualTo(expected.Email));
        Assert.That(handler.LastRequest?.Method, Is.EqualTo(HttpMethod.Get));
        Assert.That(handler.LastRequest?.RequestUri?.AbsolutePath, Is.EqualTo("/api/v1/preferences"));
    }

    [Test]
    public async Task UpdateAsync_PutsToPreferencesEndpoint()
    {
        var expected = new NotificationPreferenceDto("user@example.com", null, true, []);
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, JsonContent.Create(expected));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var client = new PreferencesHttpClient(httpClient);

        var result = await client.UpdateAsync(new UpdatePreferencesRequest("user@example.com", null, true));

        Assert.That(result.SmsOptOut, Is.True);
        Assert.That(handler.LastRequest?.Method, Is.EqualTo(HttpMethod.Put));
        Assert.That(handler.LastRequest?.RequestUri?.AbsolutePath, Is.EqualTo("/api/v1/preferences"));
    }

    [Test]
    public async Task UpdateTypeAsync_PutsToTypeEndpoint()
    {
        var expected = new NotificationTypePreferenceDto("pitstop", "recall-alert", true, true);
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, JsonContent.Create(expected));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var client = new PreferencesHttpClient(httpClient);

        var result = await client.UpdateTypeAsync("pitstop", "recall-alert", new UpdateTypePreferenceRequest(true, true));

        Assert.That(result.SmsEnabled, Is.True);
        Assert.That(handler.LastRequest?.Method, Is.EqualTo(HttpMethod.Put));
        Assert.That(handler.LastRequest?.RequestUri?.AbsolutePath, Is.EqualTo("/api/v1/preferences/types/pitstop/recall-alert"));
    }

    [Test]
    public async Task ResetTypeAsync_SendsDelete()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.NoContent);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var client = new PreferencesHttpClient(httpClient);

        await client.ResetTypeAsync("pitstop", "recall-alert");

        Assert.That(handler.LastRequest?.Method, Is.EqualTo(HttpMethod.Delete));
        Assert.That(handler.LastRequest?.RequestUri?.AbsolutePath, Is.EqualTo("/api/v1/preferences/types/pitstop/recall-alert"));
    }
}

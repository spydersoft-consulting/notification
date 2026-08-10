using System.Net;
using System.Net.Http.Json;
using Spydersoft.Notification.Contracts;

namespace Spydersoft.Notification.Client.UnitTests;

[TestFixture]
public sealed class NotificationHttpClientTests
{
    [Test]
    public async Task CreateAsync_ReturnsDto_WhenSuccessful()
    {
        var expected = new NotificationDto(
            Guid.NewGuid(), "auth0|abc", "pitstop", "recall-alert",
            "Subject", "Body", null,
            NotificationPriority.High, NotificationStatus.Created,
            false, null, DateTimeOffset.UtcNow, null, null);

        var handler = new MockHttpMessageHandler(HttpStatusCode.Created, JsonContent.Create(expected));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var client = new NotificationHttpClient(httpClient);

        var request = new CreateNotificationRequest("auth0|abc", "pitstop", "recall-alert", "Subject", "Body");
        var result = await client.CreateAsync(request);

        Assert.That(result.Id, Is.EqualTo(expected.Id));
        Assert.That(handler.LastRequest?.Method, Is.EqualTo(HttpMethod.Post));
        Assert.That(handler.LastRequest?.RequestUri?.AbsolutePath, Is.EqualTo("/api/v1/notifications"));
    }

    [Test]
    public async Task MarkReadAsync_PostsToReadEndpoint()
    {
        var expected = new NotificationDto(
            Guid.NewGuid(), "auth0|abc", "pitstop", "recall-alert",
            "Subject", "Body", null,
            NotificationPriority.Normal, NotificationStatus.Dispatched,
            true, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, null);

        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, JsonContent.Create(expected));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var client = new NotificationHttpClient(httpClient);

        var result = await client.MarkReadAsync(expected.Id);

        Assert.That(result.IsRead, Is.True);
        Assert.That(handler.LastRequest?.RequestUri?.AbsolutePath, Is.EqualTo($"/api/v1/notifications/{expected.Id}/read"));
    }

    [Test]
    public async Task GetUnreadCountAsync_ReturnsCount()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, JsonContent.Create(new { count = 3 }));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var client = new NotificationHttpClient(httpClient);

        var result = await client.GetUnreadCountAsync();

        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public async Task ListAsync_BuildsQueryString()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, JsonContent.Create(new List<NotificationDto>()));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var client = new NotificationHttpClient(httpClient);

        await client.ListAsync(unreadOnly: true, source: "pitstop", type: "recall-alert");

        var query = handler.LastRequest?.RequestUri?.Query;
        Assert.That(query, Does.Contain("unreadOnly=true"));
        Assert.That(query, Does.Contain("source=pitstop"));
        Assert.That(query, Does.Contain("type=recall-alert"));
    }
}

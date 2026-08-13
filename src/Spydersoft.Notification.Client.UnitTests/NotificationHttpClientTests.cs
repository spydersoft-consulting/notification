using System.Net;
using System.Net.Http.Json;
using System.Text;
using Spydersoft.Notification.Contracts;

namespace Spydersoft.Notification.Client.UnitTests;

[TestFixture]
public sealed class NotificationHttpClientTests
{
    [Test]
    public async Task CreateAsync_DeserializesStringEnums_AsReturnedByTheApi()
    {
        // The API serializes enums as their string names (e.g. "priority":"High"), not as
        // numbers -- JsonContent.Create(dto) in the other tests round-trips via the same
        // converter on both sides, which wouldn't have caught a mismatch. This mirrors the
        // API's actual wire format directly.
        var id = Guid.NewGuid();
        var json = $$"""
            {
              "id": "{{id}}",
              "userId": "auth0|abc",
              "source": "pitstop",
              "type": "recall-alert",
              "subject": "Subject",
              "body": "Body",
              "data": null,
              "priority": "High",
              "status": "Created",
              "isRead": false,
              "readAt": null,
              "createdAt": "2026-08-13T11:20:19.7444103+00:00",
              "entityType": "Vehicle",
              "entityId": "1",
              "deliveries": null
            }
            """;

        var handler = new MockHttpMessageHandler(
            HttpStatusCode.Created,
            new StringContent(json, Encoding.UTF8, "application/json"));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var client = new NotificationHttpClient(httpClient);

        var request = new CreateNotificationRequest("auth0|abc", "pitstop", "recall-alert", "Subject", "Body", Priority: NotificationPriority.High);
        var result = await client.CreateAsync(request);

        Assert.That(result.Id, Is.EqualTo(id));
        Assert.That(result.Priority, Is.EqualTo(NotificationPriority.High));
        Assert.That(result.Status, Is.EqualTo(NotificationStatus.Created));
    }

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

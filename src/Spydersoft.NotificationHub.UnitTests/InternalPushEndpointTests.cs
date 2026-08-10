using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Spydersoft.Notification.Contracts;

namespace Spydersoft.NotificationHub.UnitTests;

[TestFixture]
public sealed class InternalPushEndpointTests
{
    private HubWebApplicationFactory _factory = null!;

    [SetUp]
    public void SetUp() => _factory = new HubWebApplicationFactory();

    [TearDown]
    public void TearDown() => _factory.Dispose();

    [Test]
    public async Task Push_ValidToken_DeliversToTargetUsersConnection_NotToOtherUser()
    {
        await using var connectionA = await ConnectAsync("user-a");
        await using var connectionB = await ConnectAsync("user-b");

        NotificationPushDto? receivedByA = null;
        NotificationPushDto? receivedByB = null;
        connectionA.On<NotificationPushDto>("ReceiveNotification", n => receivedByA = n);
        connectionB.On<NotificationPushDto>("ReceiveNotification", n => receivedByB = n);

        var push = new HubPushRequest("user-a", new NotificationPushDto(Guid.NewGuid(), "pitstop", "recall-alert", "Subject", "Body", NotificationPriority.High, DateTimeOffset.UtcNow));
        var response = await PostPushAsync(push, HubWebApplicationFactory.InternalPushToken);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));

        await WaitUntil(() => receivedByA is not null);
        Assert.That(receivedByA!.Id, Is.EqualTo(push.Notification.Id));

        await Task.Delay(200);
        Assert.That(receivedByB, Is.Null);
    }

    [Test]
    public async Task Push_NoToken_ReturnsUnauthorized()
    {
        var push = new HubPushRequest("user-a", new NotificationPushDto(Guid.NewGuid(), "pitstop", "recall-alert", "Subject", "Body", NotificationPriority.Normal, DateTimeOffset.UtcNow));
        var response = await PostPushAsync(push, token: null);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Push_WrongToken_ReturnsUnauthorized()
    {
        var push = new HubPushRequest("user-a", new NotificationPushDto(Guid.NewGuid(), "pitstop", "recall-alert", "Subject", "Body", NotificationPriority.Normal, DateTimeOffset.UtcNow));
        var response = await PostPushAsync(push, token: "wrong-token");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    private async Task<HttpResponseMessage> PostPushAsync(HubPushRequest request, string? token)
    {
        using var client = _factory.CreateClient();
        using var message = new HttpRequestMessage(HttpMethod.Post, "/internal/push")
        {
            Content = JsonContent.Create(request),
        };
        if (token is not null)
        {
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        return await client.SendAsync(message);
    }

    private async Task<HubConnection> ConnectAsync(string userId)
    {
        var token = TestJwt.Create(userId);
        var connection = new HubConnectionBuilder()
            .WithUrl($"{_factory.Server.BaseAddress}hubs/notifications", options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
            })
            .Build();
        await connection.StartAsync();
        return connection;
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!condition())
        {
            if (cts.IsCancellationRequested)
            {
                Assert.Fail("Timed out waiting for the expected condition.");
            }
            await Task.Delay(20);
        }
    }
}

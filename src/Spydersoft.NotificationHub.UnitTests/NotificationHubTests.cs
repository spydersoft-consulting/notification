using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace Spydersoft.NotificationHub.UnitTests;

[TestFixture]
public sealed class NotificationHubTests
{
    private HubWebApplicationFactory _factory = null!;

    [SetUp]
    public void SetUp() => _factory = new HubWebApplicationFactory();

    [TearDown]
    public void TearDown() => _factory.Dispose();

    [Test]
    public async Task Connect_WithValidToken_Succeeds()
    {
        await using var connection = BuildConnection(TestJwt.Create("user-a"));

        Assert.DoesNotThrowAsync(() => connection.StartAsync());
        Assert.That(connection.State, Is.EqualTo(HubConnectionState.Connected));
    }

    [Test]
    public async Task Connect_WithoutToken_Rejected()
    {
        await using var connection = BuildConnection(accessToken: null);

        Assert.CatchAsync(() => connection.StartAsync());
    }

    private HubConnection BuildConnection(string? accessToken) =>
        new HubConnectionBuilder()
            .WithUrl($"{_factory.Server.BaseAddress}hubs/notifications", options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
                if (accessToken is not null)
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
                }
            })
            .Build();
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Spydersoft.Notification.Contracts;

namespace Spydersoft.Notification.Client.UnitTests;

[TestFixture]
public sealed class NotificationServiceCollectionExtensionsTests
{
    [Test]
    public void AddSpydersoftNotification_RegistersTypedClients()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Notification:BaseUrl"] = "https://notify.example.com",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSpydersoftNotification(config);
        var provider = services.BuildServiceProvider();

        Assert.That(provider.GetRequiredService<INotificationClient>(), Is.InstanceOf<NotificationHttpClient>());
        Assert.That(provider.GetRequiredService<IDeviceClient>(), Is.InstanceOf<DeviceHttpClient>());
        Assert.That(provider.GetRequiredService<NotificationHubConnectionFactory>(), Is.Not.Null);
    }

    [Test]
    public void AddSpydersoftNotification_MissingBaseUrl_ThrowsOnStart()
    {
        var config = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddSpydersoftNotification(config);
        var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() =>
            _ = provider.GetRequiredService<IOptions<NotificationOptions>>().Value);
    }
}

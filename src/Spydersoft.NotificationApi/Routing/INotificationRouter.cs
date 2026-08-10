namespace Spydersoft.NotificationApi.Routing;

public interface INotificationRouter
{
    Task DispatchAsync(Guid notificationId, CancellationToken cancellationToken = default);
}

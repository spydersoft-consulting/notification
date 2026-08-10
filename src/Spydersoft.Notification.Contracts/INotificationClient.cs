namespace Spydersoft.Notification.Contracts;

public interface INotificationClient
{
    Task<NotificationDto> CreateAsync(CreateNotificationRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationDto>> ListAsync(
        bool unreadOnly = false,
        string? source = null,
        string? type = null,
        int skip = 0,
        int limit = 50,
        CancellationToken cancellationToken = default);

    Task<NotificationDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<NotificationDto> MarkReadAsync(Guid id, CancellationToken cancellationToken = default);

    Task<int> MarkAllReadAsync(CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default);
}

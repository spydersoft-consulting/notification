namespace Spydersoft.Notification.Contracts;

public interface IPreferencesClient
{
    Task<NotificationPreferenceDto> GetAsync(CancellationToken cancellationToken = default);

    Task<NotificationPreferenceDto> UpdateAsync(UpdatePreferencesRequest request, CancellationToken cancellationToken = default);

    Task<NotificationTypePreferenceDto> UpdateTypeAsync(
        string source, string type, UpdateTypePreferenceRequest request, CancellationToken cancellationToken = default);

    Task ResetTypeAsync(string source, string type, CancellationToken cancellationToken = default);
}

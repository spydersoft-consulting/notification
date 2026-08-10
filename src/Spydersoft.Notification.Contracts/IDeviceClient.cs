namespace Spydersoft.Notification.Contracts;

public interface IDeviceClient
{
    Task<DeviceDto> RegisterAsync(RegisterDeviceRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DeviceDto>> ListAsync(bool includeInactive = false, CancellationToken cancellationToken = default);

    Task DeregisterAsync(Guid id, CancellationToken cancellationToken = default);
}

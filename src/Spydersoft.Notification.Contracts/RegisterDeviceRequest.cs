namespace Spydersoft.Notification.Contracts;

public sealed record RegisterDeviceRequest(DeviceType DeviceType, string Label, string? PushToken = null);

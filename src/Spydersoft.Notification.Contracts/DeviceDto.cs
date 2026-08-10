namespace Spydersoft.Notification.Contracts;

public sealed record DeviceDto(
    Guid Id,
    DeviceType DeviceType,
    string Label,
    DateTimeOffset LastSeenAt,
    DateTimeOffset RegisteredAt,
    bool IsActive);

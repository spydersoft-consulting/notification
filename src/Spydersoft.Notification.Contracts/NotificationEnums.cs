using System.Text.Json.Serialization;

namespace Spydersoft.Notification.Contracts;

[JsonConverter(typeof(JsonStringEnumConverter<NotificationPriority>))]
public enum NotificationPriority
{
    Low,
    Normal,
    High,
}

[JsonConverter(typeof(JsonStringEnumConverter<NotificationStatus>))]
public enum NotificationStatus
{
    Created,
    Dispatching,
    Dispatched,
    PartiallyFailed,
}

[JsonConverter(typeof(JsonStringEnumConverter<NotificationChannel>))]
public enum NotificationChannel
{
    InApp,
    Email,
    Sms,
}

[JsonConverter(typeof(JsonStringEnumConverter<DeliveryStatus>))]
public enum DeliveryStatus
{
    Pending,
    Sent,
    Failed,
    Skipped,
}

[JsonConverter(typeof(JsonStringEnumConverter<DeviceType>))]
public enum DeviceType
{
    Web,
    Ios,
    Android,
}

namespace Spydersoft.Notification.Contracts;

public enum NotificationPriority
{
    Low,
    Normal,
    High,
}

public enum NotificationStatus
{
    Created,
    Dispatching,
    Dispatched,
    PartiallyFailed,
}

public enum NotificationChannel
{
    InApp,
    Email,
    Sms,
}

public enum DeliveryStatus
{
    Pending,
    Sent,
    Failed,
    Skipped,
}

public enum DeviceType
{
    Web,
    Ios,
    Android,
}

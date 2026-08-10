namespace Spydersoft.Notification.Client;

public sealed class NotificationOptions
{
    public const string SectionName = "Notification";

    public string BaseUrl { get; set; } = string.Empty;

    public string? HubUrl { get; set; }
}

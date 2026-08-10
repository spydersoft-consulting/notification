namespace Spydersoft.NotificationHub;

public sealed class InternalPushOptions
{
    public const string SectionName = "Notification";

    /// <summary>Shared-secret bearer token required on POST /internal/push.</summary>
    public string? HubInternalToken { get; set; }
}

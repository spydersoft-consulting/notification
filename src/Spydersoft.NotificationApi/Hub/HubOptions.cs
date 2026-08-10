namespace Spydersoft.NotificationApi.Hub;

public sealed class HubOptions
{
    public const string SectionName = "Notification";

    /// <summary>Cluster-internal base URL of the hub's push endpoint, e.g. http://notification-hub/internal.</summary>
    public string? HubInternalUrl { get; set; }

    /// <summary>Shared-secret bearer token validated by the hub's /internal/push endpoint.</summary>
    public string? HubInternalToken { get; set; }
}

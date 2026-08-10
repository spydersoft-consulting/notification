namespace Spydersoft.Notification.Contracts;

/// <summary>Body of the hub's internal, cluster-only <c>POST /internal/push</c> endpoint.</summary>
public sealed record HubPushRequest(string UserId, NotificationPushDto Notification);

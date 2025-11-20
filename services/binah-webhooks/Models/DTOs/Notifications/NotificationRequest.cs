namespace Binah.Webhooks.Models.DTOs.Notifications;

/// <summary>
/// Request to send a notification
/// </summary>
public class NotificationRequest
{
    /// <summary>
    /// Tenant identifier
    /// </summary>
    public required string TenantId { get; set; }

    /// <summary>
    /// Notification title
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Notification message/body
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Notification type (info, success, warning, error)
    /// </summary>
    public string Type { get; set; } = "info";

    /// <summary>
    /// Channel to send notification through
    /// </summary>
    public NotificationChannel Channel { get; set; } = NotificationChannel.InApp;

    /// <summary>
    /// Recipients (email addresses, Slack user IDs, etc.)
    /// </summary>
    public List<string> Recipients { get; set; } = new();

    /// <summary>
    /// Additional metadata for the notification
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Optional link/URL for the notification
    /// </summary>
    public string? Link { get; set; }
}

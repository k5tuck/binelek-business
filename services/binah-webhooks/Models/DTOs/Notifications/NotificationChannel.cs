namespace Binah.Webhooks.Models.DTOs.Notifications;

/// <summary>
/// Supported notification channels
/// </summary>
public enum NotificationChannel
{
    /// <summary>
    /// Slack notification
    /// </summary>
    Slack,

    /// <summary>
    /// Email notification
    /// </summary>
    Email,

    /// <summary>
    /// In-app notification stored in database
    /// </summary>
    InApp,

    /// <summary>
    /// HTTP webhook notification
    /// </summary>
    Webhook
}

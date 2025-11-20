using Binah.Webhooks.Models.DTOs.Notifications;

namespace Binah.Webhooks.Services.Interfaces;

/// <summary>
/// Service for sending notifications through various channels
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Send notification when PR is created
    /// </summary>
    /// <param name="prDetails">Pull request details</param>
    /// <param name="recipients">List of recipients</param>
    /// <param name="channels">Notification channels to use</param>
    /// <returns>Task</returns>
    Task NotifyPRCreatedAsync(
        PullRequestNotificationData prDetails,
        List<string> recipients,
        List<NotificationChannel>? channels = null);

    /// <summary>
    /// Send notification when PR is merged
    /// </summary>
    /// <param name="prDetails">Pull request details</param>
    /// <param name="recipients">List of recipients</param>
    /// <param name="channels">Notification channels to use</param>
    /// <returns>Task</returns>
    Task NotifyPRMergedAsync(
        PullRequestNotificationData prDetails,
        List<string> recipients,
        List<NotificationChannel>? channels = null);

    /// <summary>
    /// Send notification when PR creation/merge fails
    /// </summary>
    /// <param name="prDetails">Pull request details</param>
    /// <param name="error">Error message</param>
    /// <param name="recipients">List of recipients</param>
    /// <param name="channels">Notification channels to use</param>
    /// <returns>Task</returns>
    Task NotifyPRFailedAsync(
        PullRequestNotificationData prDetails,
        string error,
        List<string> recipients,
        List<NotificationChannel>? channels = null);

    /// <summary>
    /// Send notification to reviewers that PR needs review
    /// </summary>
    /// <param name="prDetails">Pull request details</param>
    /// <param name="reviewers">List of reviewers</param>
    /// <param name="channels">Notification channels to use</param>
    /// <returns>Task</returns>
    Task NotifyPRNeedsReviewAsync(
        PullRequestNotificationData prDetails,
        List<string> reviewers,
        List<NotificationChannel>? channels = null);

    /// <summary>
    /// Send a generic notification
    /// </summary>
    /// <param name="request">Notification request</param>
    /// <returns>Task</returns>
    Task SendNotificationAsync(NotificationRequest request);
}

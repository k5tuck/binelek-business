using Binah.Webhooks.Models.DTOs.Notifications;
using Binah.Webhooks.Services.Interfaces;
using Binah.Webhooks.Templates;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Binah.Webhooks.Services.Implementations;

/// <summary>
/// Service for sending notifications through various channels
/// </summary>
public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public NotificationService(
        ILogger<NotificationService> logger,
        IConfiguration _configuration,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        this._configuration = _configuration;
        _httpClientFactory = httpClientFactory;
    }

    /// <inheritdoc/>
    public async Task NotifyPRCreatedAsync(
        PullRequestNotificationData prDetails,
        List<string> recipients,
        List<NotificationChannel>? channels = null)
    {
        _logger.LogInformation("Sending PR created notification for PR #{PrNumber}", prDetails.PrNumber);

        channels ??= GetDefaultChannels();

        var tasks = new List<Task>();

        foreach (var channel in channels)
        {
            var task = channel switch
            {
                NotificationChannel.Slack => SendSlackNotificationAsync(
                    NotificationTemplates.PRCreatedSlackMessage(prDetails),
                    "PR Created"),
                NotificationChannel.Email => SendEmailNotificationAsync(
                    recipients,
                    $"New PR Created: {prDetails.Title}",
                    NotificationTemplates.PRCreatedEmailHtml(prDetails),
                    NotificationTemplates.PRCreatedEmailText(prDetails)),
                NotificationChannel.InApp => StoreInAppNotificationAsync(new NotificationRequest
                {
                    TenantId = "default",
                    Title = "New PR Created",
                    Message = $"Pull request #{prDetails.PrNumber} created: {prDetails.Title}",
                    Type = "success",
                    Channel = NotificationChannel.InApp,
                    Recipients = recipients,
                    Link = prDetails.Url
                }),
                NotificationChannel.Webhook => SendWebhookNotificationAsync(new
                {
                    eventType = "pr.created",
                    data = prDetails
                }),
                _ => Task.CompletedTask
            };

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        _logger.LogInformation("PR created notifications sent successfully");
    }

    /// <inheritdoc/>
    public async Task NotifyPRMergedAsync(
        PullRequestNotificationData prDetails,
        List<string> recipients,
        List<NotificationChannel>? channels = null)
    {
        _logger.LogInformation("Sending PR merged notification for PR #{PrNumber}", prDetails.PrNumber);

        channels ??= GetDefaultChannels();

        var tasks = new List<Task>();

        foreach (var channel in channels)
        {
            var task = channel switch
            {
                NotificationChannel.Slack => SendSlackNotificationAsync(
                    NotificationTemplates.PRMergedSlackMessage(prDetails),
                    "PR Merged"),
                NotificationChannel.Email => SendEmailNotificationAsync(
                    recipients,
                    $"PR Merged: {prDetails.Title}",
                    NotificationTemplates.PRMergedEmailHtml(prDetails),
                    prDetails.Title),
                NotificationChannel.InApp => StoreInAppNotificationAsync(new NotificationRequest
                {
                    TenantId = "default",
                    Title = "PR Merged",
                    Message = $"Pull request #{prDetails.PrNumber} has been merged: {prDetails.Title}",
                    Type = "success",
                    Channel = NotificationChannel.InApp,
                    Recipients = recipients,
                    Link = prDetails.Url
                }),
                NotificationChannel.Webhook => SendWebhookNotificationAsync(new
                {
                    eventType = "pr.merged",
                    data = prDetails
                }),
                _ => Task.CompletedTask
            };

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        _logger.LogInformation("PR merged notifications sent successfully");
    }

    /// <inheritdoc/>
    public async Task NotifyPRFailedAsync(
        PullRequestNotificationData prDetails,
        string error,
        List<string> recipients,
        List<NotificationChannel>? channels = null)
    {
        _logger.LogWarning("Sending PR failed notification for PR {Title}. Error: {Error}",
            prDetails.Title, error);

        prDetails.Error = error;
        channels ??= GetDefaultChannels();

        var tasks = new List<Task>();

        foreach (var channel in channels)
        {
            var task = channel switch
            {
                NotificationChannel.Slack => SendSlackNotificationAsync(
                    NotificationTemplates.PRFailedSlackMessage(prDetails),
                    "PR Failed"),
                NotificationChannel.Email => SendEmailNotificationAsync(
                    recipients,
                    $"PR Failed: {prDetails.Title}",
                    NotificationTemplates.PRFailedEmailHtml(prDetails),
                    error),
                NotificationChannel.InApp => StoreInAppNotificationAsync(new NotificationRequest
                {
                    TenantId = "default",
                    Title = "PR Failed",
                    Message = $"Pull request creation/merge failed: {prDetails.Title}. Error: {error}",
                    Type = "error",
                    Channel = NotificationChannel.InApp,
                    Recipients = recipients,
                    Link = prDetails.Url
                }),
                NotificationChannel.Webhook => SendWebhookNotificationAsync(new
                {
                    eventType = "pr.failed",
                    data = prDetails,
                    error
                }),
                _ => Task.CompletedTask
            };

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        _logger.LogInformation("PR failed notifications sent successfully");
    }

    /// <inheritdoc/>
    public async Task NotifyPRNeedsReviewAsync(
        PullRequestNotificationData prDetails,
        List<string> reviewers,
        List<NotificationChannel>? channels = null)
    {
        _logger.LogInformation("Sending PR needs review notification for PR #{PrNumber} to {ReviewerCount} reviewers",
            prDetails.PrNumber, reviewers.Count);

        prDetails.Reviewers = reviewers;
        channels ??= GetDefaultChannels();

        var tasks = new List<Task>();

        foreach (var channel in channels)
        {
            var task = channel switch
            {
                NotificationChannel.Slack => SendSlackNotificationAsync(
                    NotificationTemplates.PRNeedsReviewSlackMessage(prDetails),
                    "PR Needs Review"),
                NotificationChannel.Email => SendEmailNotificationAsync(
                    reviewers,
                    $"Review Requested: {prDetails.Title}",
                    $"<p>Your review has been requested on PR #{prDetails.PrNumber}: {prDetails.Title}</p><p><a href=\"{prDetails.Url}\">Review Now</a></p>",
                    $"Your review has been requested on PR #{prDetails.PrNumber}: {prDetails.Title}\n\nReview at: {prDetails.Url}"),
                NotificationChannel.InApp => StoreInAppNotificationAsync(new NotificationRequest
                {
                    TenantId = "default",
                    Title = "Review Requested",
                    Message = $"Your review has been requested on PR #{prDetails.PrNumber}: {prDetails.Title}",
                    Type = "info",
                    Channel = NotificationChannel.InApp,
                    Recipients = reviewers,
                    Link = prDetails.Url
                }),
                NotificationChannel.Webhook => SendWebhookNotificationAsync(new
                {
                    eventType = "pr.review_requested",
                    data = prDetails,
                    reviewers
                }),
                _ => Task.CompletedTask
            };

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        _logger.LogInformation("PR needs review notifications sent successfully");
    }

    /// <inheritdoc/>
    public async Task SendNotificationAsync(NotificationRequest request)
    {
        _logger.LogInformation("Sending generic notification: {Title}", request.Title);

        var task = request.Channel switch
        {
            NotificationChannel.Slack => SendSlackNotificationAsync(new
            {
                text = request.Title,
                blocks = new object[]
                {
                    new
                    {
                        type = "section",
                        text = new { type = "mrkdwn", text = $"*{request.Title}*\n{request.Message}" }
                    }
                }
            }, request.Title),
            NotificationChannel.Email => SendEmailNotificationAsync(
                request.Recipients,
                request.Title,
                $"<p>{request.Message}</p>",
                request.Message),
            NotificationChannel.InApp => StoreInAppNotificationAsync(request),
            NotificationChannel.Webhook => SendWebhookNotificationAsync(request),
            _ => Task.CompletedTask
        };

        await task;

        _logger.LogInformation("Generic notification sent successfully");
    }

    // Private helper methods

    private async Task SendSlackNotificationAsync(object message, string fallbackText)
    {
        var slackWebhookUrl = _configuration["Notifications:Slack:WebhookUrl"];
        var slackEnabled = _configuration.GetValue<bool>("Notifications:Slack:Enabled", false);

        if (!slackEnabled || string.IsNullOrEmpty(slackWebhookUrl))
        {
            _logger.LogDebug("Slack notifications are disabled or webhook URL not configured");
            return;
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var json = JsonSerializer.Serialize(message);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(slackWebhookUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to send Slack notification. Status: {StatusCode}", response.StatusCode);
            }
            else
            {
                _logger.LogDebug("Slack notification sent successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Slack notification");
        }
    }

    private async Task SendEmailNotificationAsync(
        List<string> recipients,
        string subject,
        string htmlBody,
        string textBody)
    {
        var emailEnabled = _configuration.GetValue<bool>("Notifications:Email:Enabled", false);

        if (!emailEnabled || recipients.Count == 0)
        {
            _logger.LogDebug("Email notifications are disabled or no recipients provided");
            return;
        }

        try
        {
            var provider = _configuration["Notifications:Email:Provider"];

            if (provider?.ToLower() == "sendgrid")
            {
                await SendEmailViaSendGridAsync(recipients, subject, htmlBody, textBody);
            }
            else
            {
                await SendEmailViaSMTPAsync(recipients, subject, htmlBody, textBody);
            }

            _logger.LogDebug("Email notification sent to {RecipientCount} recipients", recipients.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email notification");
        }
    }

    private async Task SendEmailViaSendGridAsync(
        List<string> recipients,
        string subject,
        string htmlBody,
        string textBody)
    {
        var apiKey = _configuration["Notifications:Email:ApiKey"];
        var fromAddress = _configuration["Notifications:Email:FromAddress"];
        var fromName = _configuration["Notifications:Email:FromName"];

        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("SendGrid API key not configured");
            return;
        }

        // TODO: Implement SendGrid email sending using SendGrid SDK
        // For now, just log the action
        _logger.LogInformation("Would send email via SendGrid to {Recipients}", string.Join(", ", recipients));
        await Task.CompletedTask;
    }

    private async Task SendEmailViaSMTPAsync(
        List<string> recipients,
        string subject,
        string htmlBody,
        string textBody)
    {
        // TODO: Implement SMTP email sending using MailKit or System.Net.Mail
        // For now, just log the action
        _logger.LogInformation("Would send email via SMTP to {Recipients}", string.Join(", ", recipients));
        await Task.CompletedTask;
    }

    private async Task StoreInAppNotificationAsync(NotificationRequest request)
    {
        // TODO: Implement in-app notification storage in database
        // For now, just log the action
        _logger.LogInformation("Would store in-app notification: {Title} for {RecipientCount} recipients",
            request.Title, request.Recipients.Count);
        await Task.CompletedTask;
    }

    private async Task SendWebhookNotificationAsync(object payload)
    {
        var webhookUrl = _configuration["Notifications:Webhook:Url"];
        var webhookEnabled = _configuration.GetValue<bool>("Notifications:Webhook:Enabled", false);

        if (!webhookEnabled || string.IsNullOrEmpty(webhookUrl))
        {
            _logger.LogDebug("Webhook notifications are disabled or URL not configured");
            return;
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(webhookUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to send webhook notification. Status: {StatusCode}", response.StatusCode);
            }
            else
            {
                _logger.LogDebug("Webhook notification sent successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending webhook notification");
        }
    }

    private List<NotificationChannel> GetDefaultChannels()
    {
        var channels = new List<NotificationChannel>();

        if (_configuration.GetValue<bool>("Notifications:Slack:Enabled", false))
            channels.Add(NotificationChannel.Slack);

        if (_configuration.GetValue<bool>("Notifications:Email:Enabled", false))
            channels.Add(NotificationChannel.Email);

        if (_configuration.GetValue<bool>("Notifications:InApp:Enabled", true))
            channels.Add(NotificationChannel.InApp);

        if (_configuration.GetValue<bool>("Notifications:Webhook:Enabled", false))
            channels.Add(NotificationChannel.Webhook);

        // Default to InApp if no channels are enabled
        if (channels.Count == 0)
            channels.Add(NotificationChannel.InApp);

        return channels;
    }
}

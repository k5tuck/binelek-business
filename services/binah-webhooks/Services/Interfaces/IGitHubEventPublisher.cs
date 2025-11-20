using Binah.Webhooks.Models.Domain;
using Binah.Webhooks.Models.DTOs.GitHub;

namespace Binah.Webhooks.Services.Interfaces;

/// <summary>
/// Interface for publishing GitHub webhook events to Kafka
/// </summary>
public interface IGitHubEventPublisher
{
    /// <summary>
    /// Publish a GitHub webhook received event to Kafka
    /// </summary>
    /// <param name="webhookEvent">The GitHub webhook event from database</param>
    /// <param name="parsedEvent">Parsed GitHub webhook event DTO (optional)</param>
    /// <param name="deliveryId">GitHub delivery ID for correlation</param>
    /// <returns>Task that completes when event is published</returns>
    Task PublishWebhookReceivedAsync(
        GitHubWebhookEvent webhookEvent,
        GitHubWebhookEventDto? parsedEvent = null,
        string? deliveryId = null);
}

using Binah.Webhooks.Models.DTOs.GitHub;

namespace Binah.Webhooks.Models.Events;

/// <summary>
/// Event published to Kafka when a GitHub webhook event is received and verified
/// Follows Binah platform event schema conventions
/// </summary>
public class GitHubEventReceivedEvent
{
    /// <summary>
    /// Unique event identifier (UUID)
    /// </summary>
    public string EventId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Event type identifier (matches Kafka topic name)
    /// </summary>
    public string EventType { get; set; } = "github.event.received.v1";

    /// <summary>
    /// When the event was created (ISO 8601 format)
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Tenant identifier (UUID)
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Correlation ID for tracking related events
    /// Matches GitHub's X-GitHub-Delivery header
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Schema version for event payload
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// The actual webhook event payload
    /// </summary>
    public GitHubEventPayload Payload { get; set; } = new();
}

/// <summary>
/// GitHub webhook event payload wrapper
/// Contains metadata and the actual GitHub event data
/// </summary>
public class GitHubEventPayload
{
    /// <summary>
    /// Database ID of the stored event
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// GitHub event type (push, pull_request, issues, etc.)
    /// From X-GitHub-Event header
    /// </summary>
    public string GitHubEventType { get; set; } = string.Empty;

    /// <summary>
    /// Repository name (e.g., "k5tuck/Binelek")
    /// </summary>
    public string RepositoryName { get; set; } = string.Empty;

    /// <summary>
    /// GitHub delivery ID (X-GitHub-Delivery header)
    /// Unique identifier for this webhook delivery from GitHub
    /// </summary>
    public string DeliveryId { get; set; } = string.Empty;

    /// <summary>
    /// The raw JSON payload from GitHub
    /// Stored as string to preserve exact format
    /// </summary>
    public string RawPayload { get; set; } = string.Empty;

    /// <summary>
    /// Parsed GitHub webhook event data
    /// Contains common fields like action, repository, sender
    /// </summary>
    public GitHubWebhookEventDto? ParsedEvent { get; set; }

    /// <summary>
    /// When the webhook was received by our service
    /// </summary>
    public DateTime ReceivedAt { get; set; }

    /// <summary>
    /// Signature from GitHub for verification (X-Hub-Signature-256)
    /// </summary>
    public string Signature { get; set; } = string.Empty;
}

namespace Binah.Webhooks.Models.Domain;

/// <summary>
/// GitHub webhook event received from GitHub
/// </summary>
public class GitHubWebhookEvent
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Tenant identifier
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// GitHub event type (push, pull_request, etc.)
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Repository name (e.g., "username/repo")
    /// </summary>
    public string RepositoryName { get; set; } = string.Empty;

    /// <summary>
    /// Full webhook payload from GitHub (JSON)
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// GitHub signature for verification (X-Hub-Signature-256)
    /// </summary>
    public string Signature { get; set; } = string.Empty;

    /// <summary>
    /// When the event was received
    /// </summary>
    public DateTime ReceivedAt { get; set; }

    /// <summary>
    /// Whether the event has been processed
    /// </summary>
    public bool Processed { get; set; }

    /// <summary>
    /// When the event was processed
    /// </summary>
    public DateTime? ProcessedAt { get; set; }
}

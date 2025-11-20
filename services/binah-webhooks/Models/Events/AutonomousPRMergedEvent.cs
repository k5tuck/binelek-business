namespace Binah.Webhooks.Models.Events;

/// <summary>
/// Event published when an autonomous pull request is successfully merged
/// Topic: autonomous.pr.merged.v1
/// </summary>
public class AutonomousPRMergedEvent
{
    /// <summary>
    /// Unique event identifier (UUID)
    /// </summary>
    public string EventId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Event type identifier (matches Kafka topic name)
    /// </summary>
    public string EventType { get; set; } = "autonomous.pr.merged.v1";

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
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Schema version for event payload
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// The actual event payload
    /// </summary>
    public AutonomousPRMergedPayload Payload { get; set; } = new();
}

/// <summary>
/// Payload for autonomous PR merged event
/// </summary>
public class AutonomousPRMergedPayload
{
    /// <summary>
    /// Database ID of the autonomous PR record
    /// </summary>
    public string PrId { get; set; } = string.Empty;

    /// <summary>
    /// GitHub PR number
    /// </summary>
    public int PrNumber { get; set; }

    /// <summary>
    /// Repository name (e.g., "k5tuck/Binelek")
    /// </summary>
    public string RepositoryName { get; set; } = string.Empty;

    /// <summary>
    /// Branch name that was merged
    /// </summary>
    public string BranchName { get; set; } = string.Empty;

    /// <summary>
    /// Pull request title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Workflow type that triggered this PR
    /// </summary>
    public string WorkflowType { get; set; } = string.Empty;

    /// <summary>
    /// When the PR was merged
    /// </summary>
    public DateTime MergedAt { get; set; }

    /// <summary>
    /// Merge commit SHA
    /// </summary>
    public string? MergeCommitSha { get; set; }

    /// <summary>
    /// Whether merge was automatic (via auto-merge) or manual
    /// </summary>
    public bool AutoMerged { get; set; }
}

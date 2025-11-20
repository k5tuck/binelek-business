namespace Binah.Webhooks.Models.Events;

/// <summary>
/// Event published when autonomous pull request creation fails
/// Topic: autonomous.pr.failed.v1
/// </summary>
public class AutonomousPRFailedEvent
{
    /// <summary>
    /// Unique event identifier (UUID)
    /// </summary>
    public string EventId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Event type identifier (matches Kafka topic name)
    /// </summary>
    public string EventType { get; set; } = "autonomous.pr.failed.v1";

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
    public AutonomousPRFailedPayload Payload { get; set; } = new();
}

/// <summary>
/// Payload for autonomous PR failed event
/// </summary>
public class AutonomousPRFailedPayload
{
    /// <summary>
    /// Database ID of the autonomous PR record (if created)
    /// </summary>
    public string? PrId { get; set; }

    /// <summary>
    /// Repository name (e.g., "k5tuck/Binelek")
    /// </summary>
    public string RepositoryName { get; set; } = string.Empty;

    /// <summary>
    /// Branch name that was attempted (if created)
    /// </summary>
    public string? BranchName { get; set; }

    /// <summary>
    /// Pull request title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Workflow type that triggered this PR
    /// </summary>
    public string WorkflowType { get; set; } = string.Empty;

    /// <summary>
    /// Error message describing the failure
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Stage where the failure occurred
    /// (branch_creation, commit, pr_creation, etc.)
    /// </summary>
    public string FailureStage { get; set; } = string.Empty;

    /// <summary>
    /// Exception type if available
    /// </summary>
    public string? ExceptionType { get; set; }

    /// <summary>
    /// Stack trace for debugging (only in development)
    /// </summary>
    public string? StackTrace { get; set; }

    /// <summary>
    /// Number of files that were being changed
    /// </summary>
    public int FilesChanged { get; set; }

    /// <summary>
    /// Whether this was a retry attempt
    /// </summary>
    public bool IsRetry { get; set; }

    /// <summary>
    /// Number of retry attempts made
    /// </summary>
    public int RetryCount { get; set; }
}

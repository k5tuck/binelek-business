namespace Binah.Webhooks.Models.Domain;

/// <summary>
/// Autonomous pull request created by Claude Agent
/// </summary>
public class AutonomousPullRequest
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
    /// GitHub pull request number
    /// </summary>
    public int PrNumber { get; set; }

    /// <summary>
    /// Repository name (e.g., "username/repo")
    /// </summary>
    public string RepositoryName { get; set; } = string.Empty;

    /// <summary>
    /// Branch name containing the changes
    /// </summary>
    public string BranchName { get; set; } = string.Empty;

    /// <summary>
    /// Pull request title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Pull request description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Workflow type that triggered this PR (e.g., "sync-main-documentation")
    /// </summary>
    public string WorkflowType { get; set; } = string.Empty;

    /// <summary>
    /// Current status (open, merged, closed)
    /// </summary>
    public string Status { get; set; } = "open";

    /// <summary>
    /// When the PR was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the PR was merged
    /// </summary>
    public DateTime? MergedAt { get; set; }
}

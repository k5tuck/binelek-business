namespace Binah.Webhooks.Models.DTOs.Notifications;

/// <summary>
/// Data for pull request notifications
/// </summary>
public class PullRequestNotificationData
{
    /// <summary>
    /// Pull request number
    /// </summary>
    public int PrNumber { get; set; }

    /// <summary>
    /// Pull request title
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Repository name (owner/repo)
    /// </summary>
    public required string Repository { get; set; }

    /// <summary>
    /// Pull request URL
    /// </summary>
    public required string Url { get; set; }

    /// <summary>
    /// Pull request status (open, merged, closed)
    /// </summary>
    public string Status { get; set; } = "open";

    /// <summary>
    /// Workflow type (ontology_refactoring, code_generation, etc.)
    /// </summary>
    public string? WorkflowType { get; set; }

    /// <summary>
    /// Branch name
    /// </summary>
    public string? BranchName { get; set; }

    /// <summary>
    /// Creator username
    /// </summary>
    public string? Creator { get; set; }

    /// <summary>
    /// Error message if PR failed
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// List of reviewers
    /// </summary>
    public List<string> Reviewers { get; set; } = new();

    /// <summary>
    /// Number of commits
    /// </summary>
    public int? CommitCount { get; set; }

    /// <summary>
    /// Number of files changed
    /// </summary>
    public int? FileCount { get; set; }
}

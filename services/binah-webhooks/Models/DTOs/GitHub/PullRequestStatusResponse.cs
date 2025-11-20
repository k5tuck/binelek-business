namespace Binah.Webhooks.Models.DTOs.GitHub;

/// <summary>
/// Response containing pull request status (checks, reviews, approvals)
/// </summary>
public class PullRequestStatusResponse
{
    /// <summary>
    /// Pull request number
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// Overall status (open, closed, merged)
    /// </summary>
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Whether the PR is mergeable (no conflicts)
    /// </summary>
    public bool? IsMergeable { get; set; }

    /// <summary>
    /// Mergeable state (clean, dirty, unstable, blocked, etc.)
    /// </summary>
    public string MergeableState { get; set; } = string.Empty;

    /// <summary>
    /// Whether all required checks have passed
    /// </summary>
    public bool ChecksPassed { get; set; }

    /// <summary>
    /// List of status checks
    /// </summary>
    public List<StatusCheck> StatusChecks { get; set; } = new();

    /// <summary>
    /// Number of approvals received
    /// </summary>
    public int ApprovalsCount { get; set; }

    /// <summary>
    /// Number of changes requested
    /// </summary>
    public int ChangesRequestedCount { get; set; }

    /// <summary>
    /// List of reviews
    /// </summary>
    public List<Review> Reviews { get; set; } = new();

    /// <summary>
    /// Whether the PR meets all merge requirements
    /// </summary>
    public bool CanMerge { get; set; }

    /// <summary>
    /// Reasons why PR cannot be merged (if any)
    /// </summary>
    public List<string> BlockingReasons { get; set; } = new();
}

/// <summary>
/// Status check details
/// </summary>
public class StatusCheck
{
    /// <summary>
    /// Check name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Check status (success, failure, pending, error)
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Check conclusion (if completed)
    /// </summary>
    public string? Conclusion { get; set; }

    /// <summary>
    /// URL to check details
    /// </summary>
    public string? DetailsUrl { get; set; }
}

/// <summary>
/// Review details
/// </summary>
public class Review
{
    /// <summary>
    /// Reviewer username
    /// </summary>
    public string Reviewer { get; set; } = string.Empty;

    /// <summary>
    /// Review state (APPROVED, CHANGES_REQUESTED, COMMENTED, DISMISSED, PENDING)
    /// </summary>
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Review body/comment
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// When the review was submitted
    /// </summary>
    public DateTime? SubmittedAt { get; set; }
}

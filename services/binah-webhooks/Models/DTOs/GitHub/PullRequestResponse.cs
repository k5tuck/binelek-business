namespace Binah.Webhooks.Models.DTOs.GitHub;

/// <summary>
/// Response containing pull request details
/// </summary>
public class PullRequestResponse
{
    /// <summary>
    /// Pull request number
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// Pull request title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Pull request description/body
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// State of the pull request (open, closed, merged)
    /// </summary>
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Head branch name (source of changes)
    /// </summary>
    public string HeadBranch { get; set; } = string.Empty;

    /// <summary>
    /// Base branch name (target for changes)
    /// </summary>
    public string BaseBranch { get; set; } = string.Empty;

    /// <summary>
    /// Pull request URL
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// HTML URL for the pull request
    /// </summary>
    public string HtmlUrl { get; set; } = string.Empty;

    /// <summary>
    /// Whether the PR is a draft
    /// </summary>
    public bool IsDraft { get; set; }

    /// <summary>
    /// Whether the PR is merged
    /// </summary>
    public bool IsMerged { get; set; }

    /// <summary>
    /// Whether the PR is mergeable (no conflicts)
    /// </summary>
    public bool? IsMergeable { get; set; }

    /// <summary>
    /// Author of the pull request
    /// </summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// Created at timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Updated at timestamp
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Merged at timestamp (null if not merged)
    /// </summary>
    public DateTime? MergedAt { get; set; }

    /// <summary>
    /// Closed at timestamp (null if still open)
    /// </summary>
    public DateTime? ClosedAt { get; set; }

    /// <summary>
    /// Number of commits in the PR
    /// </summary>
    public int Commits { get; set; }

    /// <summary>
    /// Number of changed files
    /// </summary>
    public int ChangedFiles { get; set; }

    /// <summary>
    /// Number of additions
    /// </summary>
    public int Additions { get; set; }

    /// <summary>
    /// Number of deletions
    /// </summary>
    public int Deletions { get; set; }

    /// <summary>
    /// List of labels attached to the PR
    /// </summary>
    public List<string> Labels { get; set; } = new();

    /// <summary>
    /// List of requested reviewers
    /// </summary>
    public List<string> RequestedReviewers { get; set; } = new();
}

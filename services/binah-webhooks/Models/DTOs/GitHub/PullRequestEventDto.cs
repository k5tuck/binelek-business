using System.Text.Json.Serialization;

namespace Binah.Webhooks.Models.DTOs.GitHub;

/// <summary>
/// Pull request event payload
/// Triggered when a pull request is opened, closed, reopened, synchronized, etc.
/// </summary>
public class PullRequestEventDto : GitHubWebhookEventDto
{
    /// <summary>
    /// The pull request number
    /// </summary>
    [JsonPropertyName("number")]
    public int Number { get; set; }

    /// <summary>
    /// Pull request details
    /// </summary>
    [JsonPropertyName("pull_request")]
    public GitHubPullRequestDto? PullRequest { get; set; }
}

/// <summary>
/// Pull request details
/// </summary>
public class GitHubPullRequestDto
{
    /// <summary>
    /// Pull request ID
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }

    /// <summary>
    /// Pull request number
    /// </summary>
    [JsonPropertyName("number")]
    public int Number { get; set; }

    /// <summary>
    /// Pull request state (open, closed)
    /// </summary>
    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Whether the pull request is locked
    /// </summary>
    [JsonPropertyName("locked")]
    public bool Locked { get; set; }

    /// <summary>
    /// Pull request title
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Pull request description/body
    /// </summary>
    [JsonPropertyName("body")]
    public string? Body { get; set; }

    /// <summary>
    /// User who created the pull request
    /// </summary>
    [JsonPropertyName("user")]
    public GitHubUserDto? User { get; set; }

    /// <summary>
    /// Labels attached to the pull request
    /// </summary>
    [JsonPropertyName("labels")]
    public List<GitHubLabelDto> Labels { get; set; } = new();

    /// <summary>
    /// Milestone attached to the pull request
    /// </summary>
    [JsonPropertyName("milestone")]
    public GitHubMilestoneDto? Milestone { get; set; }

    /// <summary>
    /// Whether the pull request is a draft
    /// </summary>
    [JsonPropertyName("draft")]
    public bool Draft { get; set; }

    /// <summary>
    /// Head branch (source branch being merged)
    /// </summary>
    [JsonPropertyName("head")]
    public GitHubPullRequestBranchDto? Head { get; set; }

    /// <summary>
    /// Base branch (target branch)
    /// </summary>
    [JsonPropertyName("base")]
    public GitHubPullRequestBranchDto? Base { get; set; }

    /// <summary>
    /// Pull request HTML URL
    /// </summary>
    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = string.Empty;

    /// <summary>
    /// Whether the pull request has been merged
    /// </summary>
    [JsonPropertyName("merged")]
    public bool Merged { get; set; }

    /// <summary>
    /// Whether the pull request is mergeable
    /// </summary>
    [JsonPropertyName("mergeable")]
    public bool? Mergeable { get; set; }

    /// <summary>
    /// Mergeable state (clean, dirty, unstable, unknown)
    /// </summary>
    [JsonPropertyName("mergeable_state")]
    public string? MergeableState { get; set; }

    /// <summary>
    /// User who merged the pull request
    /// </summary>
    [JsonPropertyName("merged_by")]
    public GitHubUserDto? MergedBy { get; set; }

    /// <summary>
    /// Number of comments
    /// </summary>
    [JsonPropertyName("comments")]
    public int Comments { get; set; }

    /// <summary>
    /// Number of review comments
    /// </summary>
    [JsonPropertyName("review_comments")]
    public int ReviewComments { get; set; }

    /// <summary>
    /// Number of commits
    /// </summary>
    [JsonPropertyName("commits")]
    public int Commits { get; set; }

    /// <summary>
    /// Number of additions
    /// </summary>
    [JsonPropertyName("additions")]
    public int Additions { get; set; }

    /// <summary>
    /// Number of deletions
    /// </summary>
    [JsonPropertyName("deletions")]
    public int Deletions { get; set; }

    /// <summary>
    /// Number of changed files
    /// </summary>
    [JsonPropertyName("changed_files")]
    public int ChangedFiles { get; set; }

    /// <summary>
    /// When the pull request was created
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the pull request was last updated
    /// </summary>
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// When the pull request was closed (if closed)
    /// </summary>
    [JsonPropertyName("closed_at")]
    public DateTime? ClosedAt { get; set; }

    /// <summary>
    /// When the pull request was merged (if merged)
    /// </summary>
    [JsonPropertyName("merged_at")]
    public DateTime? MergedAt { get; set; }

    /// <summary>
    /// Requested reviewers
    /// </summary>
    [JsonPropertyName("requested_reviewers")]
    public List<GitHubUserDto> RequestedReviewers { get; set; } = new();

    /// <summary>
    /// Assignees
    /// </summary>
    [JsonPropertyName("assignees")]
    public List<GitHubUserDto> Assignees { get; set; } = new();
}

/// <summary>
/// Pull request branch information
/// </summary>
public class GitHubPullRequestBranchDto
{
    /// <summary>
    /// Branch label (usually username:branch)
    /// </summary>
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Branch ref (branch name)
    /// </summary>
    [JsonPropertyName("ref")]
    public string Ref { get; set; } = string.Empty;

    /// <summary>
    /// Commit SHA
    /// </summary>
    [JsonPropertyName("sha")]
    public string Sha { get; set; } = string.Empty;

    /// <summary>
    /// User who owns the branch
    /// </summary>
    [JsonPropertyName("user")]
    public GitHubUserDto? User { get; set; }

    /// <summary>
    /// Repository information
    /// </summary>
    [JsonPropertyName("repo")]
    public GitHubRepositoryDto? Repo { get; set; }
}

/// <summary>
/// GitHub label information
/// </summary>
public class GitHubLabelDto
{
    /// <summary>
    /// Label ID
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }

    /// <summary>
    /// Label name
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Label color (hex code without #)
    /// </summary>
    [JsonPropertyName("color")]
    public string Color { get; set; } = string.Empty;

    /// <summary>
    /// Label description
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// GitHub milestone information
/// </summary>
public class GitHubMilestoneDto
{
    /// <summary>
    /// Milestone ID
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }

    /// <summary>
    /// Milestone number
    /// </summary>
    [JsonPropertyName("number")]
    public int Number { get; set; }

    /// <summary>
    /// Milestone title
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Milestone description
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Milestone state (open, closed)
    /// </summary>
    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Number of open issues
    /// </summary>
    [JsonPropertyName("open_issues")]
    public int OpenIssues { get; set; }

    /// <summary>
    /// Number of closed issues
    /// </summary>
    [JsonPropertyName("closed_issues")]
    public int ClosedIssues { get; set; }

    /// <summary>
    /// When the milestone is due
    /// </summary>
    [JsonPropertyName("due_on")]
    public DateTime? DueOn { get; set; }
}

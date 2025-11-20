using System.Text.Json.Serialization;

namespace Binah.Webhooks.Models.DTOs.GitHub;

/// <summary>
/// Issues event payload
/// Triggered when an issue is opened, closed, edited, deleted, transferred, etc.
/// </summary>
public class IssuesEventDto : GitHubWebhookEventDto
{
    /// <summary>
    /// Issue details
    /// </summary>
    [JsonPropertyName("issue")]
    public GitHubIssueDto? Issue { get; set; }

    /// <summary>
    /// Changes made to the issue (for "edited" action)
    /// </summary>
    [JsonPropertyName("changes")]
    public GitHubIssueChangesDto? Changes { get; set; }

    /// <summary>
    /// User who was assigned/unassigned (for "assigned"/"unassigned" action)
    /// </summary>
    [JsonPropertyName("assignee")]
    public GitHubUserDto? Assignee { get; set; }

    /// <summary>
    /// Label that was added/removed (for "labeled"/"unlabeled" action)
    /// </summary>
    [JsonPropertyName("label")]
    public GitHubLabelDto? Label { get; set; }
}

/// <summary>
/// GitHub issue details
/// </summary>
public class GitHubIssueDto
{
    /// <summary>
    /// Issue ID
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }

    /// <summary>
    /// Issue number
    /// </summary>
    [JsonPropertyName("number")]
    public int Number { get; set; }

    /// <summary>
    /// Issue title
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Issue description/body
    /// </summary>
    [JsonPropertyName("body")]
    public string? Body { get; set; }

    /// <summary>
    /// User who created the issue
    /// </summary>
    [JsonPropertyName("user")]
    public GitHubUserDto? User { get; set; }

    /// <summary>
    /// Labels attached to the issue
    /// </summary>
    [JsonPropertyName("labels")]
    public List<GitHubLabelDto> Labels { get; set; } = new();

    /// <summary>
    /// Issue state (open, closed)
    /// </summary>
    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Whether the issue is locked
    /// </summary>
    [JsonPropertyName("locked")]
    public bool Locked { get; set; }

    /// <summary>
    /// Assignees assigned to the issue
    /// </summary>
    [JsonPropertyName("assignees")]
    public List<GitHubUserDto> Assignees { get; set; } = new();

    /// <summary>
    /// Milestone attached to the issue
    /// </summary>
    [JsonPropertyName("milestone")]
    public GitHubMilestoneDto? Milestone { get; set; }

    /// <summary>
    /// Number of comments
    /// </summary>
    [JsonPropertyName("comments")]
    public int Comments { get; set; }

    /// <summary>
    /// Issue HTML URL
    /// </summary>
    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = string.Empty;

    /// <summary>
    /// When the issue was created
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the issue was last updated
    /// </summary>
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// When the issue was closed (if closed)
    /// </summary>
    [JsonPropertyName("closed_at")]
    public DateTime? ClosedAt { get; set; }

    /// <summary>
    /// User who closed the issue
    /// </summary>
    [JsonPropertyName("closed_by")]
    public GitHubUserDto? ClosedBy { get; set; }

    /// <summary>
    /// State reason (completed, not_planned, reopened)
    /// </summary>
    [JsonPropertyName("state_reason")]
    public string? StateReason { get; set; }

    /// <summary>
    /// Pull request information (if this issue is linked to a PR)
    /// </summary>
    [JsonPropertyName("pull_request")]
    public GitHubIssuePullRequestDto? PullRequest { get; set; }
}

/// <summary>
/// Pull request reference in an issue
/// </summary>
public class GitHubIssuePullRequestDto
{
    /// <summary>
    /// Pull request URL
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Pull request HTML URL
    /// </summary>
    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = string.Empty;

    /// <summary>
    /// Pull request diff URL
    /// </summary>
    [JsonPropertyName("diff_url")]
    public string DiffUrl { get; set; } = string.Empty;

    /// <summary>
    /// Pull request patch URL
    /// </summary>
    [JsonPropertyName("patch_url")]
    public string PatchUrl { get; set; } = string.Empty;
}

/// <summary>
/// Changes made to an issue (for "edited" action)
/// </summary>
public class GitHubIssueChangesDto
{
    /// <summary>
    /// Title changes
    /// </summary>
    [JsonPropertyName("title")]
    public GitHubChangeDto? Title { get; set; }

    /// <summary>
    /// Body changes
    /// </summary>
    [JsonPropertyName("body")]
    public GitHubChangeDto? Body { get; set; }
}

/// <summary>
/// Represents a change to a field
/// </summary>
public class GitHubChangeDto
{
    /// <summary>
    /// Previous value
    /// </summary>
    [JsonPropertyName("from")]
    public string? From { get; set; }
}

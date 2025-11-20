using System.Text.Json.Serialization;

namespace Binah.Webhooks.Models.DTOs.GitHub;

/// <summary>
/// Pull request review event payload
/// Triggered when a pull request review is submitted, edited, or dismissed
/// </summary>
public class PullRequestReviewEventDto : GitHubWebhookEventDto
{
    /// <summary>
    /// Pull request that was reviewed
    /// </summary>
    [JsonPropertyName("pull_request")]
    public GitHubPullRequestDto? PullRequest { get; set; }

    /// <summary>
    /// Review details
    /// </summary>
    [JsonPropertyName("review")]
    public GitHubReviewDto? Review { get; set; }

    /// <summary>
    /// Changes made to the review (for "edited" action)
    /// </summary>
    [JsonPropertyName("changes")]
    public GitHubReviewChangesDto? Changes { get; set; }
}

/// <summary>
/// Pull request review details
/// </summary>
public class GitHubReviewDto
{
    /// <summary>
    /// Review ID
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }

    /// <summary>
    /// User who submitted the review
    /// </summary>
    [JsonPropertyName("user")]
    public GitHubUserDto? User { get; set; }

    /// <summary>
    /// Review body/comment
    /// </summary>
    [JsonPropertyName("body")]
    public string? Body { get; set; }

    /// <summary>
    /// Review state (APPROVED, CHANGES_REQUESTED, COMMENTED, DISMISSED, PENDING)
    /// </summary>
    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Review HTML URL
    /// </summary>
    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = string.Empty;

    /// <summary>
    /// Commit SHA that was reviewed
    /// </summary>
    [JsonPropertyName("commit_id")]
    public string CommitId { get; set; } = string.Empty;

    /// <summary>
    /// When the review was submitted
    /// </summary>
    [JsonPropertyName("submitted_at")]
    public DateTime? SubmittedAt { get; set; }

    /// <summary>
    /// Author association (OWNER, MEMBER, CONTRIBUTOR, etc.)
    /// </summary>
    [JsonPropertyName("author_association")]
    public string AuthorAssociation { get; set; } = string.Empty;
}

/// <summary>
/// Changes made to a review (for "edited" action)
/// </summary>
public class GitHubReviewChangesDto
{
    /// <summary>
    /// Body changes
    /// </summary>
    [JsonPropertyName("body")]
    public GitHubChangeDto? Body { get; set; }
}

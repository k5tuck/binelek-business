using System.Text.Json.Serialization;

namespace Binah.Webhooks.Models.DTOs.GitHub;

/// <summary>
/// Issue comment event payload
/// Triggered when a comment on an issue or pull request is created, edited, or deleted
/// </summary>
public class IssueCommentEventDto : GitHubWebhookEventDto
{
    /// <summary>
    /// Issue that the comment belongs to
    /// </summary>
    [JsonPropertyName("issue")]
    public GitHubIssueDto? Issue { get; set; }

    /// <summary>
    /// Comment details
    /// </summary>
    [JsonPropertyName("comment")]
    public GitHubCommentDto? Comment { get; set; }

    /// <summary>
    /// Changes made to the comment (for "edited" action)
    /// </summary>
    [JsonPropertyName("changes")]
    public GitHubCommentChangesDto? Changes { get; set; }
}

/// <summary>
/// GitHub comment details
/// </summary>
public class GitHubCommentDto
{
    /// <summary>
    /// Comment ID
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }

    /// <summary>
    /// Comment body
    /// </summary>
    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// User who created the comment
    /// </summary>
    [JsonPropertyName("user")]
    public GitHubUserDto? User { get; set; }

    /// <summary>
    /// Comment HTML URL
    /// </summary>
    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = string.Empty;

    /// <summary>
    /// When the comment was created
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the comment was last updated
    /// </summary>
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Author association (OWNER, MEMBER, CONTRIBUTOR, etc.)
    /// </summary>
    [JsonPropertyName("author_association")]
    public string AuthorAssociation { get; set; } = string.Empty;

    /// <summary>
    /// Reactions to the comment
    /// </summary>
    [JsonPropertyName("reactions")]
    public GitHubReactionsDto? Reactions { get; set; }
}

/// <summary>
/// Reactions to a comment or issue
/// </summary>
public class GitHubReactionsDto
{
    /// <summary>
    /// Total count of reactions
    /// </summary>
    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }

    /// <summary>
    /// Number of +1 reactions
    /// </summary>
    [JsonPropertyName("+1")]
    public int PlusOne { get; set; }

    /// <summary>
    /// Number of -1 reactions
    /// </summary>
    [JsonPropertyName("-1")]
    public int MinusOne { get; set; }

    /// <summary>
    /// Number of laugh reactions
    /// </summary>
    [JsonPropertyName("laugh")]
    public int Laugh { get; set; }

    /// <summary>
    /// Number of hooray reactions
    /// </summary>
    [JsonPropertyName("hooray")]
    public int Hooray { get; set; }

    /// <summary>
    /// Number of confused reactions
    /// </summary>
    [JsonPropertyName("confused")]
    public int Confused { get; set; }

    /// <summary>
    /// Number of heart reactions
    /// </summary>
    [JsonPropertyName("heart")]
    public int Heart { get; set; }

    /// <summary>
    /// Number of rocket reactions
    /// </summary>
    [JsonPropertyName("rocket")]
    public int Rocket { get; set; }

    /// <summary>
    /// Number of eyes reactions
    /// </summary>
    [JsonPropertyName("eyes")]
    public int Eyes { get; set; }
}

/// <summary>
/// Changes made to a comment (for "edited" action)
/// </summary>
public class GitHubCommentChangesDto
{
    /// <summary>
    /// Body changes
    /// </summary>
    [JsonPropertyName("body")]
    public GitHubChangeDto? Body { get; set; }
}

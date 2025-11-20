using System.Text.Json.Serialization;

namespace Binah.Webhooks.Models.DTOs.GitHub;

/// <summary>
/// Push event payload
/// Triggered when code is pushed to a repository branch
/// </summary>
public class PushEventDto : GitHubWebhookEventDto
{
    /// <summary>
    /// The full git ref that was pushed (e.g., "refs/heads/main")
    /// </summary>
    [JsonPropertyName("ref")]
    public string Ref { get; set; } = string.Empty;

    /// <summary>
    /// Git commit SHA before the push
    /// </summary>
    [JsonPropertyName("before")]
    public string Before { get; set; } = string.Empty;

    /// <summary>
    /// Git commit SHA after the push
    /// </summary>
    [JsonPropertyName("after")]
    public string After { get; set; } = string.Empty;

    /// <summary>
    /// Whether this push created the branch
    /// </summary>
    [JsonPropertyName("created")]
    public bool Created { get; set; }

    /// <summary>
    /// Whether this push deleted the branch
    /// </summary>
    [JsonPropertyName("deleted")]
    public bool Deleted { get; set; }

    /// <summary>
    /// Whether this was a forced push
    /// </summary>
    [JsonPropertyName("forced")]
    public bool Forced { get; set; }

    /// <summary>
    /// Base ref for the push (for compare URL)
    /// </summary>
    [JsonPropertyName("base_ref")]
    public string? BaseRef { get; set; }

    /// <summary>
    /// Compare URL showing the changes
    /// </summary>
    [JsonPropertyName("compare")]
    public string Compare { get; set; } = string.Empty;

    /// <summary>
    /// Array of commit objects
    /// </summary>
    [JsonPropertyName("commits")]
    public List<GitHubCommitDto> Commits { get; set; } = new();

    /// <summary>
    /// Head commit (most recent commit in the push)
    /// </summary>
    [JsonPropertyName("head_commit")]
    public GitHubCommitDto? HeadCommit { get; set; }

    /// <summary>
    /// User who pushed the commits
    /// </summary>
    [JsonPropertyName("pusher")]
    public GitHubPusherDto? Pusher { get; set; }
}

/// <summary>
/// Git commit information
/// </summary>
public class GitHubCommitDto
{
    /// <summary>
    /// Commit SHA
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Tree SHA
    /// </summary>
    [JsonPropertyName("tree_id")]
    public string TreeId { get; set; } = string.Empty;

    /// <summary>
    /// Whether this commit is distinct from previous commits
    /// </summary>
    [JsonPropertyName("distinct")]
    public bool Distinct { get; set; }

    /// <summary>
    /// Commit message
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of the commit
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Commit URL
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Commit author
    /// </summary>
    [JsonPropertyName("author")]
    public GitHubCommitAuthorDto? Author { get; set; }

    /// <summary>
    /// Commit committer
    /// </summary>
    [JsonPropertyName("committer")]
    public GitHubCommitAuthorDto? Committer { get; set; }

    /// <summary>
    /// Files added in this commit
    /// </summary>
    [JsonPropertyName("added")]
    public List<string> Added { get; set; } = new();

    /// <summary>
    /// Files removed in this commit
    /// </summary>
    [JsonPropertyName("removed")]
    public List<string> Removed { get; set; } = new();

    /// <summary>
    /// Files modified in this commit
    /// </summary>
    [JsonPropertyName("modified")]
    public List<string> Modified { get; set; } = new();
}

/// <summary>
/// Commit author/committer information
/// </summary>
public class GitHubCommitAuthorDto
{
    /// <summary>
    /// Author/committer name
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Author/committer email
    /// </summary>
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// GitHub username (if available)
    /// </summary>
    [JsonPropertyName("username")]
    public string? Username { get; set; }
}

/// <summary>
/// Pusher information
/// </summary>
public class GitHubPusherDto
{
    /// <summary>
    /// Pusher name
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Pusher email
    /// </summary>
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
}

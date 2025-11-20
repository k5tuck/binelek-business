using System.Text.Json.Serialization;

namespace Binah.Webhooks.Models.DTOs.GitHub;

/// <summary>
/// Base DTO for all GitHub webhook events
/// Contains common fields present in all GitHub webhook payloads
/// </summary>
public class GitHubWebhookEventDto
{
    /// <summary>
    /// The action that was performed (e.g., "opened", "closed", "synchronize")
    /// Not all events have an action field
    /// </summary>
    [JsonPropertyName("action")]
    public string? Action { get; set; }

    /// <summary>
    /// Repository information
    /// </summary>
    [JsonPropertyName("repository")]
    public GitHubRepositoryDto? Repository { get; set; }

    /// <summary>
    /// User who triggered the event
    /// </summary>
    [JsonPropertyName("sender")]
    public GitHubUserDto? Sender { get; set; }

    /// <summary>
    /// Installation information (for GitHub Apps)
    /// </summary>
    [JsonPropertyName("installation")]
    public GitHubInstallationDto? Installation { get; set; }

    /// <summary>
    /// Organization information (if applicable)
    /// </summary>
    [JsonPropertyName("organization")]
    public GitHubOrganizationDto? Organization { get; set; }
}

/// <summary>
/// GitHub repository information
/// </summary>
public class GitHubRepositoryDto
{
    /// <summary>
    /// Repository ID
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }

    /// <summary>
    /// Repository name (without owner)
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Full repository name (owner/repo)
    /// </summary>
    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Repository owner
    /// </summary>
    [JsonPropertyName("owner")]
    public GitHubUserDto? Owner { get; set; }

    /// <summary>
    /// Whether the repository is private
    /// </summary>
    [JsonPropertyName("private")]
    public bool IsPrivate { get; set; }

    /// <summary>
    /// Repository HTML URL
    /// </summary>
    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = string.Empty;

    /// <summary>
    /// Repository description
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Default branch name
    /// </summary>
    [JsonPropertyName("default_branch")]
    public string DefaultBranch { get; set; } = string.Empty;
}

/// <summary>
/// GitHub user information
/// </summary>
public class GitHubUserDto
{
    /// <summary>
    /// User ID
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }

    /// <summary>
    /// Username
    /// </summary>
    [JsonPropertyName("login")]
    public string Login { get; set; } = string.Empty;

    /// <summary>
    /// User type (User, Bot, Organization)
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// User avatar URL
    /// </summary>
    [JsonPropertyName("avatar_url")]
    public string AvatarUrl { get; set; } = string.Empty;

    /// <summary>
    /// User HTML URL
    /// </summary>
    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = string.Empty;
}

/// <summary>
/// GitHub installation information (for GitHub Apps)
/// </summary>
public class GitHubInstallationDto
{
    /// <summary>
    /// Installation ID
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }

    /// <summary>
    /// Node ID
    /// </summary>
    [JsonPropertyName("node_id")]
    public string NodeId { get; set; } = string.Empty;
}

/// <summary>
/// GitHub organization information
/// </summary>
public class GitHubOrganizationDto
{
    /// <summary>
    /// Organization ID
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }

    /// <summary>
    /// Organization login name
    /// </summary>
    [JsonPropertyName("login")]
    public string Login { get; set; } = string.Empty;

    /// <summary>
    /// Organization HTML URL
    /// </summary>
    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = string.Empty;
}

namespace Binah.Webhooks.Models.DTOs.GitHub;

/// <summary>
/// Request to create a new pull request
/// </summary>
public class CreatePullRequestRequest
{
    /// <summary>
    /// Title of the pull request
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Body/description of the pull request (supports Markdown)
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Name of the branch where your changes are implemented (head branch)
    /// For example: "claude/auto-refactor-property"
    /// </summary>
    public string HeadBranch { get; set; } = string.Empty;

    /// <summary>
    /// Name of the branch you want your changes pulled into (base branch)
    /// Usually "main" or "master"
    /// </summary>
    public string BaseBranch { get; set; } = "main";

    /// <summary>
    /// List of GitHub usernames to request reviews from
    /// </summary>
    public List<string> Reviewers { get; set; } = new();

    /// <summary>
    /// List of labels to add to the pull request
    /// </summary>
    public List<string> Labels { get; set; } = new();

    /// <summary>
    /// Create as a draft pull request (not ready for review)
    /// </summary>
    public bool Draft { get; set; } = false;

    /// <summary>
    /// Indicates whether maintainers can modify the pull request
    /// </summary>
    public bool MaintainerCanModify { get; set; } = true;
}

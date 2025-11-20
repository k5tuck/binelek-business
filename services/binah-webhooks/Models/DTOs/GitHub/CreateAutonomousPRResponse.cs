namespace Binah.Webhooks.Models.DTOs.GitHub;

/// <summary>
/// Response from autonomous pull request creation
/// </summary>
public class CreateAutonomousPRResponse
{
    /// <summary>
    /// Database ID of the autonomous pull request record
    /// </summary>
    public string PrId { get; set; } = string.Empty;

    /// <summary>
    /// GitHub pull request number
    /// </summary>
    public int PrNumber { get; set; }

    /// <summary>
    /// Full GitHub pull request URL
    /// </summary>
    public string PrUrl { get; set; } = string.Empty;

    /// <summary>
    /// Name of the created branch
    /// </summary>
    public string BranchName { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the pull request
    /// </summary>
    public string Status { get; set; } = "open";

    /// <summary>
    /// When the PR was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Error message if PR creation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Whether the PR was created successfully
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// SHA of the commit that was created
    /// </summary>
    public string? CommitSha { get; set; }

    /// <summary>
    /// Number of files changed
    /// </summary>
    public int FilesChanged { get; set; }

    /// <summary>
    /// Whether reviewers were successfully requested
    /// </summary>
    public bool ReviewersRequested { get; set; }

    /// <summary>
    /// Whether labels were successfully added
    /// </summary>
    public bool LabelsAdded { get; set; }
}

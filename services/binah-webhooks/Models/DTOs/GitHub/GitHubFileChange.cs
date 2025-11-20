namespace Binah.Webhooks.Models.DTOs.GitHub;

/// <summary>
/// Represents a file change to be committed to GitHub
/// </summary>
public class GitHubFileChange
{
    /// <summary>
    /// Path to the file in the repository (e.g., "schemas/property.yaml")
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Content of the file (for Add/Update operations)
    /// Base64-encoded for binary files, plain text otherwise
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Type of change to make
    /// </summary>
    public GitHubFileChangeMode Mode { get; set; } = GitHubFileChangeMode.Add;

    /// <summary>
    /// SHA of the file blob (required for Update/Delete operations)
    /// </summary>
    public string? Sha { get; set; }
}

/// <summary>
/// Type of file change operation
/// </summary>
public enum GitHubFileChangeMode
{
    /// <summary>
    /// Add a new file to the repository
    /// </summary>
    Add,

    /// <summary>
    /// Update an existing file in the repository
    /// </summary>
    Update,

    /// <summary>
    /// Delete an existing file from the repository
    /// </summary>
    Delete
}

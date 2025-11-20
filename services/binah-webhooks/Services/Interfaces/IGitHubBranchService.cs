using Octokit;

namespace Binah.Webhooks.Services.Interfaces;

/// <summary>
/// Service for GitHub branch operations
/// </summary>
public interface IGitHubBranchService
{
    /// <summary>
    /// Get branch information
    /// </summary>
    /// <param name="owner">Repository owner (e.g., "k5tuck")</param>
    /// <param name="repo">Repository name (e.g., "Binelek")</param>
    /// <param name="branchName">Branch name (e.g., "main")</param>
    /// <returns>Branch information</returns>
    Task<Branch> GetBranchAsync(string owner, string repo, string branchName);

    /// <summary>
    /// Create a new branch from a specific commit SHA
    /// </summary>
    /// <param name="owner">Repository owner</param>
    /// <param name="repo">Repository name</param>
    /// <param name="branchName">Name for the new branch (e.g., "claude/feature-123")</param>
    /// <param name="fromSha">SHA of the commit to branch from</param>
    /// <returns>Reference to the created branch</returns>
    Task<Reference> CreateBranchAsync(string owner, string repo, string branchName, string fromSha);

    /// <summary>
    /// Delete a branch
    /// </summary>
    /// <param name="owner">Repository owner</param>
    /// <param name="repo">Repository name</param>
    /// <param name="branchName">Branch name to delete</param>
    Task DeleteBranchAsync(string owner, string repo, string branchName);

    /// <summary>
    /// Check if a branch exists
    /// </summary>
    /// <param name="owner">Repository owner</param>
    /// <param name="repo">Repository name</param>
    /// <param name="branchName">Branch name to check</param>
    /// <returns>True if branch exists, false otherwise</returns>
    Task<bool> BranchExistsAsync(string owner, string repo, string branchName);

    /// <summary>
    /// Get the default branch for a repository (usually "main" or "master")
    /// </summary>
    /// <param name="owner">Repository owner</param>
    /// <param name="repo">Repository name</param>
    /// <returns>Default branch name</returns>
    Task<string> GetDefaultBranchAsync(string owner, string repo);

    /// <summary>
    /// Get the SHA of the HEAD commit on a branch
    /// </summary>
    /// <param name="owner">Repository owner</param>
    /// <param name="repo">Repository name</param>
    /// <param name="branchName">Branch name</param>
    /// <returns>SHA of the HEAD commit</returns>
    Task<string> GetBranchHeadShaAsync(string owner, string repo, string branchName);
}

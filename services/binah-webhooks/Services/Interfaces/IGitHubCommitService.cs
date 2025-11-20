using Binah.Webhooks.Models.DTOs.GitHub;
using Octokit;

namespace Binah.Webhooks.Services.Interfaces;

/// <summary>
/// Service for GitHub commit operations
/// </summary>
public interface IGitHubCommitService
{
    /// <summary>
    /// Create a commit with multiple file changes
    /// Uses Git Tree API for atomic multi-file commits
    /// </summary>
    /// <param name="owner">Repository owner</param>
    /// <param name="repo">Repository name</param>
    /// <param name="branchName">Branch to commit to</param>
    /// <param name="message">Commit message</param>
    /// <param name="files">List of file changes</param>
    /// <returns>SHA of the created commit</returns>
    Task<string> CreateCommitAsync(
        string owner,
        string repo,
        string branchName,
        string message,
        List<GitHubFileChange> files);

    /// <summary>
    /// Get commit details
    /// </summary>
    /// <param name="owner">Repository owner</param>
    /// <param name="repo">Repository name</param>
    /// <param name="sha">Commit SHA</param>
    /// <returns>Commit information</returns>
    Task<GitHubCommit> GetCommitAsync(string owner, string repo, string sha);

    /// <summary>
    /// Update a single file in the repository
    /// </summary>
    /// <param name="owner">Repository owner</param>
    /// <param name="repo">Repository name</param>
    /// <param name="path">File path</param>
    /// <param name="content">New file content</param>
    /// <param name="message">Commit message</param>
    /// <param name="branchName">Branch name</param>
    /// <param name="sha">Current file SHA (for conflict detection)</param>
    /// <returns>Updated file information</returns>
    Task<RepositoryContentChangeSet> UpdateFileAsync(
        string owner,
        string repo,
        string path,
        string content,
        string message,
        string branchName,
        string sha);

    /// <summary>
    /// Create a new file in the repository
    /// </summary>
    /// <param name="owner">Repository owner</param>
    /// <param name="repo">Repository name</param>
    /// <param name="path">File path</param>
    /// <param name="content">File content</param>
    /// <param name="message">Commit message</param>
    /// <param name="branchName">Branch name</param>
    /// <returns>Created file information</returns>
    Task<RepositoryContentChangeSet> CreateFileAsync(
        string owner,
        string repo,
        string path,
        string content,
        string message,
        string branchName);

    /// <summary>
    /// Delete a file from the repository
    /// </summary>
    /// <param name="owner">Repository owner</param>
    /// <param name="repo">Repository name</param>
    /// <param name="path">File path</param>
    /// <param name="message">Commit message</param>
    /// <param name="branchName">Branch name</param>
    /// <param name="sha">Current file SHA</param>
    Task DeleteFileAsync(
        string owner,
        string repo,
        string path,
        string message,
        string branchName,
        string sha);

    /// <summary>
    /// Get file content from repository
    /// </summary>
    /// <param name="owner">Repository owner</param>
    /// <param name="repo">Repository name</param>
    /// <param name="path">File path</param>
    /// <param name="branchName">Branch name (optional, defaults to default branch)</param>
    /// <returns>File content and metadata</returns>
    Task<RepositoryContent> GetFileAsync(
        string owner,
        string repo,
        string path,
        string? branchName = null);
}

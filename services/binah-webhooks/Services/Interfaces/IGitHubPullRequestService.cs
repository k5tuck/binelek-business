using Binah.Webhooks.Models.DTOs.GitHub;
using Octokit;

namespace Binah.Webhooks.Services.Interfaces;

/// <summary>
/// Interface for GitHub pull request operations using Octokit.NET
/// </summary>
public interface IGitHubPullRequestService
{
    /// <summary>
    /// Create a new pull request
    /// </summary>
    /// <param name="owner">Repository owner</param>
    /// <param name="repo">Repository name</param>
    /// <param name="request">Pull request creation details</param>
    /// <param name="tenantId">Tenant ID for token resolution</param>
    /// <returns>Pull request response</returns>
    Task<PullRequestResponse> CreatePullRequestAsync(
        string owner,
        string repo,
        CreatePullRequestRequest request,
        string tenantId);

    /// <summary>
    /// Get pull request details
    /// </summary>
    /// <param name="owner">Repository owner</param>
    /// <param name="repo">Repository name</param>
    /// <param name="prNumber">Pull request number</param>
    /// <param name="tenantId">Tenant ID for token resolution</param>
    /// <returns>Pull request details</returns>
    Task<PullRequestResponse> GetPullRequestAsync(
        string owner,
        string repo,
        int prNumber,
        string tenantId);

    /// <summary>
    /// Update an existing pull request
    /// </summary>
    /// <param name="owner">Repository owner</param>
    /// <param name="repo">Repository name</param>
    /// <param name="prNumber">Pull request number</param>
    /// <param name="title">Updated title (optional)</param>
    /// <param name="body">Updated body/description (optional)</param>
    /// <param name="tenantId">Tenant ID for token resolution</param>
    /// <returns>Updated pull request details</returns>
    Task<PullRequestResponse> UpdatePullRequestAsync(
        string owner,
        string repo,
        int prNumber,
        string? title = null,
        string? body = null,
        string? tenantId = null);

    /// <summary>
    /// Merge a pull request
    /// </summary>
    /// <param name="owner">Repository owner</param>
    /// <param name="repo">Repository name</param>
    /// <param name="prNumber">Pull request number</param>
    /// <param name="request">Merge request details</param>
    /// <param name="tenantId">Tenant ID for token resolution</param>
    /// <returns>Merge result</returns>
    Task<bool> MergePullRequestAsync(
        string owner,
        string repo,
        int prNumber,
        MergePullRequestRequest request,
        string tenantId);

    /// <summary>
    /// Close a pull request without merging
    /// </summary>
    /// <param name="owner">Repository owner</param>
    /// <param name="repo">Repository name</param>
    /// <param name="prNumber">Pull request number</param>
    /// <param name="tenantId">Tenant ID for token resolution</param>
    /// <returns>True if closed successfully</returns>
    Task<bool> ClosePullRequestAsync(
        string owner,
        string repo,
        int prNumber,
        string tenantId);

    /// <summary>
    /// Add a comment to a pull request
    /// </summary>
    /// <param name="owner">Repository owner</param>
    /// <param name="repo">Repository name</param>
    /// <param name="prNumber">Pull request number</param>
    /// <param name="comment">Comment text</param>
    /// <param name="tenantId">Tenant ID for token resolution</param>
    /// <returns>Comment ID</returns>
    Task<long> AddCommentAsync(
        string owner,
        string repo,
        int prNumber,
        string comment,
        string tenantId);

    /// <summary>
    /// Request reviewers for a pull request
    /// </summary>
    /// <param name="owner">Repository owner</param>
    /// <param name="repo">Repository name</param>
    /// <param name="prNumber">Pull request number</param>
    /// <param name="reviewers">List of reviewer usernames</param>
    /// <param name="tenantId">Tenant ID for token resolution</param>
    /// <returns>True if reviewers added successfully</returns>
    Task<bool> RequestReviewersAsync(
        string owner,
        string repo,
        int prNumber,
        List<string> reviewers,
        string tenantId);

    /// <summary>
    /// Add labels to a pull request
    /// </summary>
    /// <param name="owner">Repository owner</param>
    /// <param name="repo">Repository name</param>
    /// <param name="prNumber">Pull request number</param>
    /// <param name="labels">List of label names</param>
    /// <param name="tenantId">Tenant ID for token resolution</param>
    /// <returns>True if labels added successfully</returns>
    Task<bool> AddLabelsAsync(
        string owner,
        string repo,
        int prNumber,
        List<string> labels,
        string tenantId);

    /// <summary>
    /// Get pull request status (checks, reviews, approval status)
    /// </summary>
    /// <param name="owner">Repository owner</param>
    /// <param name="repo">Repository name</param>
    /// <param name="prNumber">Pull request number</param>
    /// <param name="tenantId">Tenant ID for token resolution</param>
    /// <returns>PR status details</returns>
    Task<PullRequestStatusResponse> GetPullRequestStatusAsync(
        string owner,
        string repo,
        int prNumber,
        string tenantId);
}

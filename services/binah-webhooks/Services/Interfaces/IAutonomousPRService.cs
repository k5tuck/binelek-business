using Binah.Webhooks.Models.DTOs.GitHub;
using Binah.Webhooks.Models.Domain;

namespace Binah.Webhooks.Services.Interfaces;

/// <summary>
/// Service for orchestrating autonomous pull request creation
/// Handles the complete workflow: branch creation, commit, PR creation, and tracking
/// </summary>
public interface IAutonomousPRService
{
    /// <summary>
    /// Create an autonomous pull request with complete workflow orchestration
    ///
    /// Workflow:
    /// 1. Generate unique branch name
    /// 2. Check if branch exists (cleanup if needed)
    /// 3. Get default branch SHA
    /// 4. Create new branch
    /// 5. Commit all files (multi-file atomic commit)
    /// 6. Generate PR description from template
    /// 7. Create pull request
    /// 8. Store PR in database
    /// 9. Publish Kafka event (autonomous.pr.created.v1)
    /// 10. If AutoMerge: Wait for CI checks, merge if green
    /// </summary>
    /// <param name="request">Autonomous PR creation request</param>
    /// <returns>PR creation response with PR number and URL</returns>
    Task<CreateAutonomousPRResponse> CreateAutonomousPRAsync(CreateAutonomousPRRequest request);

    /// <summary>
    /// Get the status of an autonomous pull request
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="prId">Pull request database ID</param>
    /// <returns>Current PR status</returns>
    Task<CreateAutonomousPRResponse> GetPRStatusAsync(string tenantId, string prId);

    /// <summary>
    /// List all autonomous pull requests for a tenant
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="status">Optional status filter (open, merged, closed)</param>
    /// <returns>List of autonomous pull requests</returns>
    Task<IEnumerable<AutonomousPullRequest>> ListAutonomousPRsAsync(string tenantId, string? status = null);

    /// <summary>
    /// Retry a failed autonomous pull request
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="prId">Pull request database ID</param>
    /// <returns>Updated PR creation response</returns>
    Task<CreateAutonomousPRResponse> RetryFailedPRAsync(string tenantId, string prId);

    /// <summary>
    /// Close an autonomous pull request without merging
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="prId">Pull request database ID</param>
    /// <returns>True if closed successfully</returns>
    Task<bool> CloseAutonomousPRAsync(string tenantId, string prId);

    /// <summary>
    /// Merge an autonomous pull request
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="prId">Pull request database ID</param>
    /// <param name="commitMessage">Optional custom merge commit message</param>
    /// <returns>True if merged successfully</returns>
    Task<bool> MergeAutonomousPRAsync(string tenantId, string prId, string? commitMessage = null);
}

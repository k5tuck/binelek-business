using Binah.Webhooks.Models.Domain;

namespace Binah.Webhooks.Repositories.Interfaces;

/// <summary>
/// Repository for autonomous pull requests
/// </summary>
public interface IAutonomousPullRequestRepository
{
    /// <summary>
    /// Create a new autonomous pull request record
    /// </summary>
    Task<AutonomousPullRequest> CreateAsync(AutonomousPullRequest pullRequest);

    /// <summary>
    /// Get pull request by ID
    /// </summary>
    Task<AutonomousPullRequest?> GetByIdAsync(Guid id);

    /// <summary>
    /// Get all pull requests for a tenant
    /// </summary>
    Task<IEnumerable<AutonomousPullRequest>> GetByTenantAsync(Guid tenantId);

    /// <summary>
    /// Get pull request by PR number and repository
    /// </summary>
    Task<AutonomousPullRequest?> GetByPrNumberAsync(Guid tenantId, string repositoryName, int prNumber);

    /// <summary>
    /// Update pull request status
    /// </summary>
    Task UpdateStatusAsync(Guid id, string status, DateTime? mergedAt = null);

    /// <summary>
    /// Get open pull requests for a tenant
    /// </summary>
    Task<IEnumerable<AutonomousPullRequest>> GetOpenByTenantAsync(Guid tenantId);
}

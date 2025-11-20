using Binah.Webhooks.Models.Domain;

namespace Binah.Webhooks.Repositories.Interfaces;

/// <summary>
/// Repository for GitHub OAuth tokens
/// </summary>
public interface IGitHubOAuthTokenRepository
{
    /// <summary>
    /// Create or update OAuth token for a tenant
    /// </summary>
    Task<GitHubOAuthToken> UpsertAsync(GitHubOAuthToken token);

    /// <summary>
    /// Get OAuth token by tenant ID
    /// </summary>
    Task<GitHubOAuthToken?> GetByTenantAsync(Guid tenantId);

    /// <summary>
    /// Delete OAuth token for a tenant
    /// </summary>
    Task DeleteAsync(Guid tenantId);

    /// <summary>
    /// Check if tenant has OAuth token
    /// </summary>
    Task<bool> ExistsAsync(Guid tenantId);
}

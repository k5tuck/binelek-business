using Octokit;

namespace Binah.Webhooks.Services.Interfaces;

/// <summary>
/// GitHub API client wrapper interface
/// </summary>
public interface IGitHubApiClient
{
    /// <summary>
    /// Initialize GitHub client for a specific tenant using their OAuth token
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <returns>True if initialization successful, false if no token found</returns>
    Task<bool> InitializeForTenantAsync(Guid tenantId);

    /// <summary>
    /// Get the authenticated user information
    /// </summary>
    /// <returns>Authenticated user details</returns>
    /// <exception cref="InvalidOperationException">If client not initialized</exception>
    Task<User> GetAuthenticatedUserAsync();

    /// <summary>
    /// Get repository information
    /// </summary>
    /// <param name="owner">Repository owner</param>
    /// <param name="name">Repository name</param>
    /// <returns>Repository details</returns>
    /// <exception cref="InvalidOperationException">If client not initialized</exception>
    Task<Repository> GetRepositoryAsync(string owner, string name);

    /// <summary>
    /// Check if client is initialized and ready to use
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Get the current tenant ID for which the client is initialized
    /// </summary>
    Guid? CurrentTenantId { get; }
}

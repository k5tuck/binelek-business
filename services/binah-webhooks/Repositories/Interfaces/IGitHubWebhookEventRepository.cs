using Binah.Webhooks.Models.Domain;

namespace Binah.Webhooks.Repositories.Interfaces;

/// <summary>
/// Repository for GitHub webhook events
/// </summary>
public interface IGitHubWebhookEventRepository
{
    /// <summary>
    /// Create a new GitHub webhook event
    /// </summary>
    Task<GitHubWebhookEvent> CreateAsync(GitHubWebhookEvent webhookEvent);

    /// <summary>
    /// Get a GitHub webhook event by ID
    /// </summary>
    Task<GitHubWebhookEvent?> GetByIdAsync(Guid id);

    /// <summary>
    /// Get all GitHub webhook events for a tenant
    /// </summary>
    Task<IEnumerable<GitHubWebhookEvent>> GetByTenantAsync(Guid tenantId, int limit = 100);

    /// <summary>
    /// Get unprocessed events for a tenant
    /// </summary>
    Task<IEnumerable<GitHubWebhookEvent>> GetUnprocessedByTenantAsync(Guid tenantId);

    /// <summary>
    /// Mark a webhook event as processed
    /// </summary>
    Task MarkAsProcessedAsync(Guid id);

    /// <summary>
    /// Get events by repository name
    /// </summary>
    Task<IEnumerable<GitHubWebhookEvent>> GetByRepositoryAsync(Guid tenantId, string repositoryName, int limit = 100);
}

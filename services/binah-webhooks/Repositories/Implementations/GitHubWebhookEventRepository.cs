using Binah.Webhooks.Models;
using Binah.Webhooks.Models.Domain;
using Binah.Webhooks.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Binah.Webhooks.Repositories.Implementations;

/// <summary>
/// Repository implementation for GitHub webhook events
/// </summary>
public class GitHubWebhookEventRepository : IGitHubWebhookEventRepository
{
    private readonly WebhookDbContext _context;

    public GitHubWebhookEventRepository(WebhookDbContext context)
    {
        _context = context;
    }

    public async Task<GitHubWebhookEvent> CreateAsync(GitHubWebhookEvent webhookEvent)
    {
        _context.GitHubWebhookEvents.Add(webhookEvent);
        await _context.SaveChangesAsync();
        return webhookEvent;
    }

    public async Task<GitHubWebhookEvent?> GetByIdAsync(Guid id)
    {
        return await _context.GitHubWebhookEvents
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<IEnumerable<GitHubWebhookEvent>> GetByTenantAsync(Guid tenantId, int limit = 100)
    {
        return await _context.GitHubWebhookEvents
            .Where(e => e.TenantId == tenantId)
            .OrderByDescending(e => e.ReceivedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<GitHubWebhookEvent>> GetUnprocessedByTenantAsync(Guid tenantId)
    {
        return await _context.GitHubWebhookEvents
            .Where(e => e.TenantId == tenantId && !e.Processed)
            .OrderBy(e => e.ReceivedAt)
            .ToListAsync();
    }

    public async Task MarkAsProcessedAsync(Guid id)
    {
        var webhookEvent = await _context.GitHubWebhookEvents.FindAsync(id);
        if (webhookEvent != null)
        {
            webhookEvent.Processed = true;
            webhookEvent.ProcessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<GitHubWebhookEvent>> GetByRepositoryAsync(Guid tenantId, string repositoryName, int limit = 100)
    {
        return await _context.GitHubWebhookEvents
            .Where(e => e.TenantId == tenantId && e.RepositoryName == repositoryName)
            .OrderByDescending(e => e.ReceivedAt)
            .Take(limit)
            .ToListAsync();
    }
}

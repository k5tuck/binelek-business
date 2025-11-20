using Binah.Webhooks.Models;
using Binah.Webhooks.Models.Domain;
using Binah.Webhooks.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Binah.Webhooks.Repositories.Implementations;

/// <summary>
/// Repository implementation for autonomous pull requests
/// </summary>
public class AutonomousPullRequestRepository : IAutonomousPullRequestRepository
{
    private readonly WebhookDbContext _context;

    public AutonomousPullRequestRepository(WebhookDbContext context)
    {
        _context = context;
    }

    public async Task<AutonomousPullRequest> CreateAsync(AutonomousPullRequest pullRequest)
    {
        _context.AutonomousPullRequests.Add(pullRequest);
        await _context.SaveChangesAsync();
        return pullRequest;
    }

    public async Task<AutonomousPullRequest?> GetByIdAsync(Guid id)
    {
        return await _context.AutonomousPullRequests
            .FirstOrDefaultAsync(pr => pr.Id == id);
    }

    public async Task<IEnumerable<AutonomousPullRequest>> GetByTenantAsync(Guid tenantId)
    {
        return await _context.AutonomousPullRequests
            .Where(pr => pr.TenantId == tenantId)
            .OrderByDescending(pr => pr.CreatedAt)
            .ToListAsync();
    }

    public async Task<AutonomousPullRequest?> GetByPrNumberAsync(Guid tenantId, string repositoryName, int prNumber)
    {
        return await _context.AutonomousPullRequests
            .FirstOrDefaultAsync(pr =>
                pr.TenantId == tenantId &&
                pr.RepositoryName == repositoryName &&
                pr.PrNumber == prNumber);
    }

    public async Task UpdateStatusAsync(Guid id, string status, DateTime? mergedAt = null)
    {
        var pullRequest = await _context.AutonomousPullRequests.FindAsync(id);
        if (pullRequest != null)
        {
            pullRequest.Status = status;
            if (mergedAt.HasValue)
            {
                pullRequest.MergedAt = mergedAt.Value;
            }
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<AutonomousPullRequest>> GetOpenByTenantAsync(Guid tenantId)
    {
        return await _context.AutonomousPullRequests
            .Where(pr => pr.TenantId == tenantId && pr.Status == "open")
            .OrderByDescending(pr => pr.CreatedAt)
            .ToListAsync();
    }
}

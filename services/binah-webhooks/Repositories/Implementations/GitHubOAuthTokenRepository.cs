using Binah.Webhooks.Models;
using Binah.Webhooks.Models.Domain;
using Binah.Webhooks.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Binah.Webhooks.Repositories.Implementations;

/// <summary>
/// Repository implementation for GitHub OAuth tokens
/// </summary>
public class GitHubOAuthTokenRepository : IGitHubOAuthTokenRepository
{
    private readonly WebhookDbContext _context;

    public GitHubOAuthTokenRepository(WebhookDbContext context)
    {
        _context = context;
    }

    public async Task<GitHubOAuthToken> UpsertAsync(GitHubOAuthToken token)
    {
        var existing = await _context.GitHubOAuthTokens
            .FirstOrDefaultAsync(t => t.TenantId == token.TenantId);

        if (existing != null)
        {
            // Update existing token
            existing.AccessToken = token.AccessToken;
            existing.TokenType = token.TokenType;
            existing.Scope = token.Scope;
            existing.ExpiresAt = token.ExpiresAt;
            existing.RefreshToken = token.RefreshToken;
            existing.CreatedAt = DateTime.UtcNow; // Update timestamp
        }
        else
        {
            // Create new token
            _context.GitHubOAuthTokens.Add(token);
        }

        await _context.SaveChangesAsync();
        return existing ?? token;
    }

    public async Task<GitHubOAuthToken?> GetByTenantAsync(Guid tenantId)
    {
        return await _context.GitHubOAuthTokens
            .FirstOrDefaultAsync(t => t.TenantId == tenantId);
    }

    public async Task DeleteAsync(Guid tenantId)
    {
        var token = await _context.GitHubOAuthTokens
            .FirstOrDefaultAsync(t => t.TenantId == tenantId);

        if (token != null)
        {
            _context.GitHubOAuthTokens.Remove(token);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(Guid tenantId)
    {
        return await _context.GitHubOAuthTokens
            .AnyAsync(t => t.TenantId == tenantId);
    }
}

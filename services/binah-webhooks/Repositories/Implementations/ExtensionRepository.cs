using Binah.Webhooks.Models;
using Binah.Webhooks.Models.Domain;
using Binah.Webhooks.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Binah.Webhooks.Repositories.Implementations;

/// <summary>
/// Repository implementation for extension catalog
/// </summary>
public class ExtensionRepository : IExtensionRepository
{
    private readonly WebhookDbContext _context;

    public ExtensionRepository(WebhookDbContext context)
    {
        _context = context;
    }

    public async Task<List<Extension>> GetAllAsync(string? category = null, string? search = null)
    {
        var query = _context.Extensions
            .Where(e => e.IsPublished)
            .AsQueryable();

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(e => e.Category == category);
        }

        if (!string.IsNullOrEmpty(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(e =>
                e.Name.ToLower().Contains(searchLower) ||
                e.Description.ToLower().Contains(searchLower) ||
                e.Author.ToLower().Contains(searchLower));
        }

        return await query
            .OrderByDescending(e => e.IsOfficial)
            .ThenByDescending(e => e.InstallCount)
            .ToListAsync();
    }

    public async Task<Extension?> GetByIdAsync(Guid id)
    {
        return await _context.Extensions.FindAsync(id);
    }

    public async Task<Extension> CreateAsync(Extension extension)
    {
        extension.CreatedAt = DateTime.UtcNow;
        extension.UpdatedAt = DateTime.UtcNow;
        _context.Extensions.Add(extension);
        await _context.SaveChangesAsync();
        return extension;
    }

    public async Task<Extension> UpdateAsync(Extension extension)
    {
        extension.UpdatedAt = DateTime.UtcNow;
        _context.Extensions.Update(extension);
        await _context.SaveChangesAsync();
        return extension;
    }

    public async Task DeleteAsync(Guid id)
    {
        var extension = await _context.Extensions.FindAsync(id);
        if (extension != null)
        {
            _context.Extensions.Remove(extension);
            await _context.SaveChangesAsync();
        }
    }

    public async Task IncrementInstallCountAsync(Guid id)
    {
        var extension = await _context.Extensions.FindAsync(id);
        if (extension != null)
        {
            extension.InstallCount++;
            await _context.SaveChangesAsync();
        }
    }

    public async Task DecrementInstallCountAsync(Guid id)
    {
        var extension = await _context.Extensions.FindAsync(id);
        if (extension != null && extension.InstallCount > 0)
        {
            extension.InstallCount--;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<Extension>> GetByCategoryAsync(string category)
    {
        return await _context.Extensions
            .Where(e => e.IsPublished && e.Category == category)
            .OrderByDescending(e => e.InstallCount)
            .ToListAsync();
    }

    public async Task<List<Extension>> GetFeaturedAsync()
    {
        return await _context.Extensions
            .Where(e => e.IsPublished && e.IsOfficial)
            .OrderByDescending(e => e.InstallCount)
            .Take(10)
            .ToListAsync();
    }
}

/// <summary>
/// Repository implementation for installed extensions
/// </summary>
public class InstalledExtensionRepository : IInstalledExtensionRepository
{
    private readonly WebhookDbContext _context;

    public InstalledExtensionRepository(WebhookDbContext context)
    {
        _context = context;
    }

    public async Task<List<InstalledExtension>> GetByTenantAsync(Guid tenantId)
    {
        return await _context.InstalledExtensions
            .Include(i => i.Extension)
            .Where(i => i.TenantId == tenantId)
            .OrderByDescending(i => i.InstalledAt)
            .ToListAsync();
    }

    public async Task<InstalledExtension?> GetByIdAsync(Guid id, Guid tenantId)
    {
        return await _context.InstalledExtensions
            .Include(i => i.Extension)
            .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId);
    }

    public async Task<InstalledExtension?> GetByExtensionIdAsync(Guid extensionId, Guid tenantId)
    {
        return await _context.InstalledExtensions
            .Include(i => i.Extension)
            .FirstOrDefaultAsync(i => i.ExtensionId == extensionId && i.TenantId == tenantId);
    }

    public async Task<InstalledExtension> CreateAsync(InstalledExtension installation)
    {
        installation.InstalledAt = DateTime.UtcNow;
        installation.UpdatedAt = DateTime.UtcNow;
        _context.InstalledExtensions.Add(installation);
        await _context.SaveChangesAsync();
        return installation;
    }

    public async Task<InstalledExtension> UpdateAsync(InstalledExtension installation)
    {
        installation.UpdatedAt = DateTime.UtcNow;
        _context.InstalledExtensions.Update(installation);
        await _context.SaveChangesAsync();
        return installation;
    }

    public async Task DeleteAsync(Guid id, Guid tenantId)
    {
        var installation = await _context.InstalledExtensions
            .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId);

        if (installation != null)
        {
            _context.InstalledExtensions.Remove(installation);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> IsInstalledAsync(Guid extensionId, Guid tenantId)
    {
        return await _context.InstalledExtensions
            .AnyAsync(i => i.ExtensionId == extensionId && i.TenantId == tenantId);
    }

    public async Task<List<InstalledExtension>> GetActiveByTenantAsync(Guid tenantId)
    {
        return await _context.InstalledExtensions
            .Include(i => i.Extension)
            .Where(i => i.TenantId == tenantId && i.Status == "active")
            .ToListAsync();
    }
}

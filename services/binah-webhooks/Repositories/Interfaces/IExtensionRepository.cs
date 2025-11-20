using Binah.Webhooks.Models.Domain;

namespace Binah.Webhooks.Repositories.Interfaces;

/// <summary>
/// Repository for extension catalog operations
/// </summary>
public interface IExtensionRepository
{
    /// <summary>
    /// Get all published extensions
    /// </summary>
    Task<List<Extension>> GetAllAsync(string? category = null, string? search = null);

    /// <summary>
    /// Get extension by ID
    /// </summary>
    Task<Extension?> GetByIdAsync(Guid id);

    /// <summary>
    /// Create a new extension
    /// </summary>
    Task<Extension> CreateAsync(Extension extension);

    /// <summary>
    /// Update an extension
    /// </summary>
    Task<Extension> UpdateAsync(Extension extension);

    /// <summary>
    /// Delete an extension
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Increment install count
    /// </summary>
    Task IncrementInstallCountAsync(Guid id);

    /// <summary>
    /// Decrement install count
    /// </summary>
    Task DecrementInstallCountAsync(Guid id);

    /// <summary>
    /// Get extensions by category
    /// </summary>
    Task<List<Extension>> GetByCategoryAsync(string category);

    /// <summary>
    /// Get featured/official extensions
    /// </summary>
    Task<List<Extension>> GetFeaturedAsync();
}

/// <summary>
/// Repository for installed extension operations
/// </summary>
public interface IInstalledExtensionRepository
{
    /// <summary>
    /// Get all installed extensions for a tenant
    /// </summary>
    Task<List<InstalledExtension>> GetByTenantAsync(Guid tenantId);

    /// <summary>
    /// Get specific installed extension
    /// </summary>
    Task<InstalledExtension?> GetByIdAsync(Guid id, Guid tenantId);

    /// <summary>
    /// Get installed extension by extension ID
    /// </summary>
    Task<InstalledExtension?> GetByExtensionIdAsync(Guid extensionId, Guid tenantId);

    /// <summary>
    /// Install an extension for a tenant
    /// </summary>
    Task<InstalledExtension> CreateAsync(InstalledExtension installation);

    /// <summary>
    /// Update installed extension configuration
    /// </summary>
    Task<InstalledExtension> UpdateAsync(InstalledExtension installation);

    /// <summary>
    /// Uninstall an extension
    /// </summary>
    Task DeleteAsync(Guid id, Guid tenantId);

    /// <summary>
    /// Check if extension is installed for tenant
    /// </summary>
    Task<bool> IsInstalledAsync(Guid extensionId, Guid tenantId);

    /// <summary>
    /// Get active extensions for tenant
    /// </summary>
    Task<List<InstalledExtension>> GetActiveByTenantAsync(Guid tenantId);
}

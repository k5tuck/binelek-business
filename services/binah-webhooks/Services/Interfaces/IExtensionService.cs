using Binah.Webhooks.Models.Domain;

namespace Binah.Webhooks.Services.Interfaces;

/// <summary>
/// Service for managing extension marketplace
/// </summary>
public interface IExtensionService
{
    /// <summary>
    /// Get all available extensions in the catalog
    /// </summary>
    Task<List<ExtensionDto>> GetCatalogAsync(string? category = null, string? search = null);

    /// <summary>
    /// Get extension details by ID
    /// </summary>
    Task<ExtensionDto?> GetExtensionAsync(Guid extensionId);

    /// <summary>
    /// Get featured/official extensions
    /// </summary>
    Task<List<ExtensionDto>> GetFeaturedAsync();

    /// <summary>
    /// Get available categories
    /// </summary>
    Task<List<string>> GetCategoriesAsync();

    /// <summary>
    /// Get installed extensions for a tenant
    /// </summary>
    Task<List<InstalledExtensionDto>> GetInstalledAsync(Guid tenantId);

    /// <summary>
    /// Install an extension for a tenant
    /// </summary>
    Task<InstalledExtensionDto> InstallAsync(Guid tenantId, Guid extensionId, Guid userId, string? tenantTier);

    /// <summary>
    /// Update extension configuration
    /// </summary>
    Task<InstalledExtensionDto> UpdateConfigAsync(Guid tenantId, Guid installationId, Dictionary<string, object> config);

    /// <summary>
    /// Uninstall an extension
    /// </summary>
    Task UninstallAsync(Guid tenantId, Guid installationId);

    /// <summary>
    /// Enable an extension
    /// </summary>
    Task<InstalledExtensionDto> EnableAsync(Guid tenantId, Guid installationId);

    /// <summary>
    /// Disable an extension
    /// </summary>
    Task<InstalledExtensionDto> DisableAsync(Guid tenantId, Guid installationId);

    /// <summary>
    /// Check if tenant can install extension based on tier
    /// </summary>
    Task<bool> CanInstallAsync(Guid extensionId, string tenantTier);
}

/// <summary>
/// Extension DTO for API responses
/// </summary>
public class ExtensionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string RequiredTier { get; set; } = string.Empty;
    public int InstallCount { get; set; }
    public double Rating { get; set; }
    public int RatingCount { get; set; }
    public string? IconUrl { get; set; }
    public Dictionary<string, object> DefaultConfig { get; set; } = new();
    public bool IsOfficial { get; set; }
    public List<string> Tags { get; set; } = new();
    public string? DocumentationUrl { get; set; }
    public string? SupportUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Installed extension DTO for API responses
/// </summary>
public class InstalledExtensionDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ExtensionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public Dictionary<string, object> Config { get; set; } = new();
    public string InstalledVersion { get; set; } = string.Empty;
    public DateTime InstalledAt { get; set; }
    public Guid InstalledBy { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ExtensionDto? Extension { get; set; }
}

/// <summary>
/// Request to install an extension
/// </summary>
public class InstallExtensionRequest
{
    public Guid ExtensionId { get; set; }
    public Dictionary<string, object>? InitialConfig { get; set; }
}

/// <summary>
/// Request to update extension configuration
/// </summary>
public class UpdateExtensionConfigRequest
{
    public Dictionary<string, object> Config { get; set; } = new();
}

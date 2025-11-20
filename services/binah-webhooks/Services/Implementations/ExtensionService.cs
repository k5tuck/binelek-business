using System.Text.Json;
using Binah.Webhooks.Models.Domain;
using Binah.Webhooks.Repositories.Interfaces;
using Binah.Webhooks.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Binah.Webhooks.Services.Implementations;

/// <summary>
/// Service implementation for extension marketplace
/// </summary>
public class ExtensionService : IExtensionService
{
    private readonly IExtensionRepository _extensionRepository;
    private readonly IInstalledExtensionRepository _installedExtensionRepository;
    private readonly ILogger<ExtensionService> _logger;

    // Tier hierarchy for permission checking
    private static readonly Dictionary<string, int> TierLevels = new()
    {
        { "solo", 1 },
        { "team", 2 },
        { "business", 3 },
        { "enterprise", 4 }
    };

    public ExtensionService(
        IExtensionRepository extensionRepository,
        IInstalledExtensionRepository installedExtensionRepository,
        ILogger<ExtensionService> logger)
    {
        _extensionRepository = extensionRepository;
        _installedExtensionRepository = installedExtensionRepository;
        _logger = logger;
    }

    public async Task<List<ExtensionDto>> GetCatalogAsync(string? category = null, string? search = null)
    {
        var extensions = await _extensionRepository.GetAllAsync(category, search);
        return extensions.Select(MapToDto).ToList();
    }

    public async Task<ExtensionDto?> GetExtensionAsync(Guid extensionId)
    {
        var extension = await _extensionRepository.GetByIdAsync(extensionId);
        return extension != null ? MapToDto(extension) : null;
    }

    public async Task<List<ExtensionDto>> GetFeaturedAsync()
    {
        var extensions = await _extensionRepository.GetFeaturedAsync();
        return extensions.Select(MapToDto).ToList();
    }

    public async Task<List<string>> GetCategoriesAsync()
    {
        // Return predefined categories
        return await Task.FromResult(new List<string>
        {
            "analytics",
            "integration",
            "ai",
            "automation",
            "visualization",
            "security",
            "communication",
            "data"
        });
    }

    public async Task<List<InstalledExtensionDto>> GetInstalledAsync(Guid tenantId)
    {
        var installations = await _installedExtensionRepository.GetByTenantAsync(tenantId);
        return installations.Select(MapToInstalledDto).ToList();
    }

    public async Task<InstalledExtensionDto> InstallAsync(
        Guid tenantId,
        Guid extensionId,
        Guid userId,
        string? tenantTier)
    {
        // Check if extension exists
        var extension = await _extensionRepository.GetByIdAsync(extensionId);
        if (extension == null)
        {
            throw new InvalidOperationException($"Extension {extensionId} not found");
        }

        // Check tier requirements
        if (!string.IsNullOrEmpty(tenantTier) && !CanInstallForTier(extension.RequiredTier, tenantTier))
        {
            throw new InvalidOperationException(
                $"Extension requires {extension.RequiredTier} tier or higher. Current tier: {tenantTier}");
        }

        // Check if already installed
        var existing = await _installedExtensionRepository.IsInstalledAsync(extensionId, tenantId);
        if (existing)
        {
            throw new InvalidOperationException($"Extension {extensionId} is already installed");
        }

        // Create installation
        var installation = new InstalledExtension
        {
            TenantId = tenantId,
            ExtensionId = extensionId,
            Status = "active",
            Config = extension.DefaultConfig,
            InstalledVersion = extension.Version,
            InstalledBy = userId
        };

        var created = await _installedExtensionRepository.CreateAsync(installation);

        // Increment install count
        await _extensionRepository.IncrementInstallCountAsync(extensionId);

        _logger.LogInformation(
            "Extension {ExtensionId} installed for tenant {TenantId} by user {UserId}",
            extensionId, tenantId, userId);

        // Load the extension for the response
        created.Extension = extension;
        return MapToInstalledDto(created);
    }

    public async Task<InstalledExtensionDto> UpdateConfigAsync(
        Guid tenantId,
        Guid installationId,
        Dictionary<string, object> config)
    {
        var installation = await _installedExtensionRepository.GetByIdAsync(installationId, tenantId);
        if (installation == null)
        {
            throw new InvalidOperationException($"Installation {installationId} not found");
        }

        installation.Config = JsonSerializer.Serialize(config);
        var updated = await _installedExtensionRepository.UpdateAsync(installation);

        _logger.LogInformation(
            "Extension configuration updated for installation {InstallationId}",
            installationId);

        return MapToInstalledDto(updated);
    }

    public async Task UninstallAsync(Guid tenantId, Guid installationId)
    {
        var installation = await _installedExtensionRepository.GetByIdAsync(installationId, tenantId);
        if (installation == null)
        {
            throw new InvalidOperationException($"Installation {installationId} not found");
        }

        await _installedExtensionRepository.DeleteAsync(installationId, tenantId);

        // Decrement install count
        await _extensionRepository.DecrementInstallCountAsync(installation.ExtensionId);

        _logger.LogInformation(
            "Extension uninstalled: installation {InstallationId} for tenant {TenantId}",
            installationId, tenantId);
    }

    public async Task<InstalledExtensionDto> EnableAsync(Guid tenantId, Guid installationId)
    {
        var installation = await _installedExtensionRepository.GetByIdAsync(installationId, tenantId);
        if (installation == null)
        {
            throw new InvalidOperationException($"Installation {installationId} not found");
        }

        installation.Status = "active";
        var updated = await _installedExtensionRepository.UpdateAsync(installation);

        _logger.LogInformation("Extension enabled: installation {InstallationId}", installationId);

        return MapToInstalledDto(updated);
    }

    public async Task<InstalledExtensionDto> DisableAsync(Guid tenantId, Guid installationId)
    {
        var installation = await _installedExtensionRepository.GetByIdAsync(installationId, tenantId);
        if (installation == null)
        {
            throw new InvalidOperationException($"Installation {installationId} not found");
        }

        installation.Status = "disabled";
        var updated = await _installedExtensionRepository.UpdateAsync(installation);

        _logger.LogInformation("Extension disabled: installation {InstallationId}", installationId);

        return MapToInstalledDto(updated);
    }

    public async Task<bool> CanInstallAsync(Guid extensionId, string tenantTier)
    {
        var extension = await _extensionRepository.GetByIdAsync(extensionId);
        if (extension == null)
        {
            return false;
        }

        return CanInstallForTier(extension.RequiredTier, tenantTier);
    }

    private bool CanInstallForTier(string requiredTier, string tenantTier)
    {
        var requiredLevel = TierLevels.GetValueOrDefault(requiredTier.ToLower(), 1);
        var tenantLevel = TierLevels.GetValueOrDefault(tenantTier.ToLower(), 1);
        return tenantLevel >= requiredLevel;
    }

    private ExtensionDto MapToDto(Extension extension)
    {
        return new ExtensionDto
        {
            Id = extension.Id,
            Name = extension.Name,
            Description = extension.Description,
            Author = extension.Author,
            Version = extension.Version,
            Category = extension.Category,
            RequiredTier = extension.RequiredTier,
            InstallCount = extension.InstallCount,
            Rating = extension.Rating,
            RatingCount = extension.RatingCount,
            IconUrl = extension.IconUrl,
            DefaultConfig = DeserializeJson(extension.DefaultConfig),
            IsOfficial = extension.IsOfficial,
            Tags = DeserializeJsonArray(extension.Tags),
            DocumentationUrl = extension.DocumentationUrl,
            SupportUrl = extension.SupportUrl,
            CreatedAt = extension.CreatedAt,
            UpdatedAt = extension.UpdatedAt
        };
    }

    private InstalledExtensionDto MapToInstalledDto(InstalledExtension installation)
    {
        return new InstalledExtensionDto
        {
            Id = installation.Id,
            TenantId = installation.TenantId,
            ExtensionId = installation.ExtensionId,
            Status = installation.Status,
            Config = DeserializeJson(installation.Config),
            InstalledVersion = installation.InstalledVersion,
            InstalledAt = installation.InstalledAt,
            InstalledBy = installation.InstalledBy,
            UpdatedAt = installation.UpdatedAt,
            Extension = installation.Extension != null ? MapToDto(installation.Extension) : null
        };
    }

    private Dictionary<string, object> DeserializeJson(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new();
        }
        catch
        {
            return new();
        }
    }

    private List<string> DeserializeJsonArray(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new();
        }
        catch
        {
            return new();
        }
    }
}

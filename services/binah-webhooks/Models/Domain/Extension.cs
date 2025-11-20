namespace Binah.Webhooks.Models.Domain;

/// <summary>
/// Extension available in the marketplace catalog
/// </summary>
public class Extension
{
    /// <summary>
    /// Unique identifier for the extension
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Display name of the extension
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the extension
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Author/publisher of the extension
    /// </summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// Semantic version of the extension
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Category for filtering (e.g., "analytics", "integration", "ai")
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Minimum subscription tier required (solo, team, business, enterprise)
    /// </summary>
    public string RequiredTier { get; set; } = "solo";

    /// <summary>
    /// Total number of installations
    /// </summary>
    public int InstallCount { get; set; } = 0;

    /// <summary>
    /// Average rating (0-5)
    /// </summary>
    public double Rating { get; set; } = 0;

    /// <summary>
    /// Number of ratings
    /// </summary>
    public int RatingCount { get; set; } = 0;

    /// <summary>
    /// URL to the extension icon
    /// </summary>
    public string? IconUrl { get; set; }

    /// <summary>
    /// Default configuration for the extension (JSON)
    /// </summary>
    public string DefaultConfig { get; set; } = "{}";

    /// <summary>
    /// Whether this is an official Binah extension
    /// </summary>
    public bool IsOfficial { get; set; } = false;

    /// <summary>
    /// Whether the extension is published and visible
    /// </summary>
    public bool IsPublished { get; set; } = true;

    /// <summary>
    /// Tags for search and filtering (JSON array)
    /// </summary>
    public string Tags { get; set; } = "[]";

    /// <summary>
    /// Documentation URL
    /// </summary>
    public string? DocumentationUrl { get; set; }

    /// <summary>
    /// Support URL or email
    /// </summary>
    public string? SupportUrl { get; set; }

    /// <summary>
    /// When the extension was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the extension was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Installed extension for a specific tenant
/// </summary>
public class InstalledExtension
{
    /// <summary>
    /// Unique identifier for the installation
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Tenant that installed the extension
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Reference to the extension
    /// </summary>
    public Guid ExtensionId { get; set; }

    /// <summary>
    /// Current status (active, disabled, pending_upgrade)
    /// </summary>
    public string Status { get; set; } = "active";

    /// <summary>
    /// Tenant-specific configuration (JSON)
    /// </summary>
    public string Config { get; set; } = "{}";

    /// <summary>
    /// Installed version
    /// </summary>
    public string InstalledVersion { get; set; } = "1.0.0";

    /// <summary>
    /// When the extension was installed
    /// </summary>
    public DateTime InstalledAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who installed the extension
    /// </summary>
    public Guid InstalledBy { get; set; }

    /// <summary>
    /// When the extension was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the extension
    /// </summary>
    public Extension? Extension { get; set; }
}

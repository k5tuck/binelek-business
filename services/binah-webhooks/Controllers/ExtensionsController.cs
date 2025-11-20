using Binah.Webhooks.Services.Interfaces;
using Binah.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Binah.Webhooks.Controllers;

/// <summary>
/// Controller for extension marketplace management
/// </summary>
[ApiController]
[Route("api/extensions")]
public class ExtensionsController : ControllerBase
{
    private readonly IExtensionService _extensionService;
    private readonly ILogger<ExtensionsController> _logger;

    public ExtensionsController(
        IExtensionService extensionService,
        ILogger<ExtensionsController> logger)
    {
        _extensionService = extensionService ?? throw new ArgumentNullException(nameof(extensionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get all available extensions in the catalog
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<ExtensionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<ExtensionDto>>>> GetCatalog(
        [FromQuery] string? category = null,
        [FromQuery] string? search = null)
    {
        var extensions = await _extensionService.GetCatalogAsync(category, search);
        return Ok(ApiResponse<List<ExtensionDto>>.Ok(extensions));
    }

    /// <summary>
    /// Get extension details by ID
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ExtensionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ExtensionDto>>> GetExtension(Guid id)
    {
        var extension = await _extensionService.GetExtensionAsync(id);

        if (extension == null)
        {
            return NotFound(ApiResponse<ExtensionDto>.WithError("Extension not found"));
        }

        return Ok(ApiResponse<ExtensionDto>.Ok(extension));
    }

    /// <summary>
    /// Get featured/official extensions
    /// </summary>
    [HttpGet("featured")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<ExtensionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<ExtensionDto>>>> GetFeatured()
    {
        var extensions = await _extensionService.GetFeaturedAsync();
        return Ok(ApiResponse<List<ExtensionDto>>.Ok(extensions));
    }

    /// <summary>
    /// Get available extension categories
    /// </summary>
    [HttpGet("categories")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<string>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<string>>>> GetCategories()
    {
        var categories = await _extensionService.GetCategoriesAsync();
        return Ok(ApiResponse<List<string>>.Ok(categories));
    }

    /// <summary>
    /// Get installed extensions for a tenant
    /// </summary>
    [HttpGet("~/api/tenants/{tenantId}/extensions")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<List<InstalledExtensionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<List<InstalledExtensionDto>>>> GetInstalled(Guid tenantId)
    {
        var userTenantId = User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(userTenantId) || !Guid.TryParse(userTenantId, out var parsedTenantId))
        {
            return BadRequest(ApiResponse<List<InstalledExtensionDto>>.WithError("Tenant ID not found"));
        }

        // Verify tenant access
        if (parsedTenantId != tenantId)
        {
            return Forbid();
        }

        var installations = await _extensionService.GetInstalledAsync(tenantId);
        return Ok(ApiResponse<List<InstalledExtensionDto>>.Ok(installations));
    }

    /// <summary>
    /// Install an extension for a tenant
    /// </summary>
    [HttpPost("~/api/tenants/{tenantId}/extensions")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<InstalledExtensionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<InstalledExtensionDto>>> Install(
        Guid tenantId,
        [FromBody] InstallExtensionRequest request)
    {
        var userTenantId = User.FindFirst("tenant_id")?.Value;
        var userIdClaim = User.FindFirst("sub")?.Value;
        var tierClaim = User.FindFirst("tier")?.Value ?? "solo";

        if (string.IsNullOrEmpty(userTenantId) || !Guid.TryParse(userTenantId, out var parsedTenantId))
        {
            return BadRequest(ApiResponse<InstalledExtensionDto>.WithError("Tenant ID not found"));
        }

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return BadRequest(ApiResponse<InstalledExtensionDto>.WithError("User ID not found"));
        }

        // Verify tenant access
        if (parsedTenantId != tenantId)
        {
            return Forbid();
        }

        try
        {
            var installation = await _extensionService.InstallAsync(
                tenantId,
                request.ExtensionId,
                userId,
                tierClaim);

            return Ok(ApiResponse<InstalledExtensionDto>.Ok(installation));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<InstalledExtensionDto>.WithError(ex.Message));
        }
    }

    /// <summary>
    /// Update extension configuration
    /// </summary>
    [HttpPut("~/api/tenants/{tenantId}/extensions/{id}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<InstalledExtensionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<InstalledExtensionDto>>> UpdateConfig(
        Guid tenantId,
        Guid id,
        [FromBody] UpdateExtensionConfigRequest request)
    {
        var userTenantId = User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(userTenantId) || !Guid.TryParse(userTenantId, out var parsedTenantId))
        {
            return BadRequest(ApiResponse<InstalledExtensionDto>.WithError("Tenant ID not found"));
        }

        // Verify tenant access
        if (parsedTenantId != tenantId)
        {
            return Forbid();
        }

        try
        {
            var installation = await _extensionService.UpdateConfigAsync(tenantId, id, request.Config);
            return Ok(ApiResponse<InstalledExtensionDto>.Ok(installation));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ApiResponse<InstalledExtensionDto>.WithError(ex.Message));
        }
    }

    /// <summary>
    /// Uninstall an extension
    /// </summary>
    [HttpDelete("~/api/tenants/{tenantId}/extensions/{id}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<bool>>> Uninstall(Guid tenantId, Guid id)
    {
        var userTenantId = User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(userTenantId) || !Guid.TryParse(userTenantId, out var parsedTenantId))
        {
            return BadRequest(ApiResponse<bool>.WithError("Tenant ID not found"));
        }

        // Verify tenant access
        if (parsedTenantId != tenantId)
        {
            return Forbid();
        }

        try
        {
            await _extensionService.UninstallAsync(tenantId, id);
            return Ok(ApiResponse<bool>.Ok(true));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ApiResponse<bool>.WithError(ex.Message));
        }
    }

    /// <summary>
    /// Enable an extension
    /// </summary>
    [HttpPost("~/api/tenants/{tenantId}/extensions/{id}/enable")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<InstalledExtensionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<InstalledExtensionDto>>> Enable(Guid tenantId, Guid id)
    {
        var userTenantId = User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(userTenantId) || !Guid.TryParse(userTenantId, out var parsedTenantId))
        {
            return BadRequest(ApiResponse<InstalledExtensionDto>.WithError("Tenant ID not found"));
        }

        // Verify tenant access
        if (parsedTenantId != tenantId)
        {
            return Forbid();
        }

        try
        {
            var installation = await _extensionService.EnableAsync(tenantId, id);
            return Ok(ApiResponse<InstalledExtensionDto>.Ok(installation));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ApiResponse<InstalledExtensionDto>.WithError(ex.Message));
        }
    }

    /// <summary>
    /// Disable an extension
    /// </summary>
    [HttpPost("~/api/tenants/{tenantId}/extensions/{id}/disable")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<InstalledExtensionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<InstalledExtensionDto>>> Disable(Guid tenantId, Guid id)
    {
        var userTenantId = User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(userTenantId) || !Guid.TryParse(userTenantId, out var parsedTenantId))
        {
            return BadRequest(ApiResponse<InstalledExtensionDto>.WithError("Tenant ID not found"));
        }

        // Verify tenant access
        if (parsedTenantId != tenantId)
        {
            return Forbid();
        }

        try
        {
            var installation = await _extensionService.DisableAsync(tenantId, id);
            return Ok(ApiResponse<InstalledExtensionDto>.Ok(installation));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ApiResponse<InstalledExtensionDto>.WithError(ex.Message));
        }
    }

    /// <summary>
    /// Check if tenant can install an extension
    /// </summary>
    [HttpGet("{id}/can-install")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> CanInstall(Guid id)
    {
        var tierClaim = User.FindFirst("tier")?.Value ?? "solo";
        var canInstall = await _extensionService.CanInstallAsync(id, tierClaim);
        return Ok(ApiResponse<bool>.Ok(canInstall));
    }
}

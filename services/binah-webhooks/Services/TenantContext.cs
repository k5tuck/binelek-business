using System.Security.Claims;

namespace Binah.Webhooks.Services;

/// <summary>
/// Implementation of ITenantContext that extracts tenant information from HTTP context
/// </summary>
public class TenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<TenantContext> _logger;

    public TenantContext(IHttpContextAccessor httpContextAccessor, ILogger<TenantContext> logger)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the current tenant ID from JWT claims
    /// NOTE: Uses snake_case "tenant_id" claim (not camelCase "tenantId")
    /// </summary>
    public string TenantId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null)
            {
                _logger.LogWarning("No user context available");
                return string.Empty;
            }

            // NOTE: binah-auth uses "tenant_id" claim name (not "tenantId")
            var tenantId = user.FindFirst("tenant_id")?.Value;

            if (string.IsNullOrEmpty(tenantId))
            {
                _logger.LogWarning("tenant_id claim not found in JWT token");
            }

            return tenantId ?? string.Empty;
        }
    }

    /// <summary>
    /// Gets the current user ID from JWT claims (sub claim)
    /// </summary>
    public string UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? user?.FindFirst("sub")?.Value
                   ?? string.Empty;
        }
    }

    /// <summary>
    /// Gets the current user's role from JWT claims
    /// </summary>
    public string Role
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.FindFirst(ClaimTypes.Role)?.Value
                   ?? user?.FindFirst("role")?.Value
                   ?? "user";
        }
    }

    /// <summary>
    /// Checks if the current user has a specific permission
    /// </summary>
    public bool HasPermission(string permission)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null) return false;

        var permissions = user.FindFirst("permissions")?.Value;
        if (string.IsNullOrEmpty(permissions)) return false;

        return permissions.Split(',').Contains(permission);
    }
}

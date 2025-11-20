using System.Security.Claims;

namespace Binah.Webhooks.Middleware;

/// <summary>
/// Middleware that validates tenant context from JWT claims
/// </summary>
public class TenantContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantContextMiddleware> _logger;

    public TenantContextMiddleware(RequestDelegate next, ILogger<TenantContextMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip tenant validation for health check and swagger endpoints
        var path = context.Request.Path.Value?.ToLower() ?? string.Empty;
        if (path.Contains("/health") ||
            path.Contains("/swagger") ||
            path.Contains("/api-docs"))
        {
            await _next(context);
            return;
        }

        // If user is authenticated, validate tenant context
        if (context.User.Identity?.IsAuthenticated == true)
        {
            // NOTE: binah-auth uses "tenant_id" claim name (not "tenantId")
            var tenantId = context.User.FindFirst("tenant_id")?.Value;

            if (string.IsNullOrEmpty(tenantId))
            {
                _logger.LogWarning("Authenticated request missing tenant_id claim. Path: {Path}", path);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Missing tenant context",
                    message = "JWT token must contain tenant_id claim"
                });
                return;
            }

            // Log tenant context for debugging
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? context.User.FindFirst("sub")?.Value;

            _logger.LogDebug("Request authenticated for tenant: {TenantId}, user: {UserId}", tenantId, userId);

            // Store tenant ID in HttpContext items for easy access
            context.Items["TenantId"] = tenantId;
            context.Items["UserId"] = userId;
        }

        await _next(context);
    }
}

namespace Binah.Webhooks.Services;

/// <summary>
/// Provides access to the current tenant context
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// Gets the current tenant ID from the JWT token
    /// </summary>
    string TenantId { get; }

    /// <summary>
    /// Gets the current user ID from the JWT token
    /// </summary>
    string UserId { get; }

    /// <summary>
    /// Gets the current user's role from the JWT token
    /// </summary>
    string Role { get; }

    /// <summary>
    /// Checks if the current user has a specific permission
    /// </summary>
    bool HasPermission(string permission);
}

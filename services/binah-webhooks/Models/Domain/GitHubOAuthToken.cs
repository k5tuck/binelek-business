namespace Binah.Webhooks.Models.Domain;

/// <summary>
/// GitHub OAuth access token for a tenant
/// </summary>
public class GitHubOAuthToken
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Tenant identifier (unique - one token per tenant)
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// GitHub OAuth access token
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Token type (typically "Bearer")
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// OAuth scopes granted
    /// </summary>
    public string? Scope { get; set; }

    /// <summary>
    /// When the token was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the token expires (null if no expiration)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Refresh token for token renewal
    /// </summary>
    public string? RefreshToken { get; set; }
}

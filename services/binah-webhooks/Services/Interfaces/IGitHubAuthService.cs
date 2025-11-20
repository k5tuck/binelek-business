namespace Binah.Webhooks.Services.Interfaces;

/// <summary>
/// GitHub OAuth authentication service interface
/// </summary>
public interface IGitHubAuthService
{
    /// <summary>
    /// Generate GitHub OAuth authorization URL for a tenant
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="state">CSRF protection state parameter</param>
    /// <returns>GitHub authorization URL</returns>
    string GetAuthorizationUrl(Guid tenantId, string state);

    /// <summary>
    /// Exchange authorization code for OAuth access token
    /// </summary>
    /// <param name="code">Authorization code from GitHub callback</param>
    /// <param name="state">State parameter for CSRF verification</param>
    /// <returns>Tenant ID for which the token was stored</returns>
    /// <exception cref="InvalidOperationException">If state validation fails or token exchange fails</exception>
    Task<Guid> ExchangeCodeForTokenAsync(string code, string state);

    /// <summary>
    /// Refresh an expired OAuth token for a tenant
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <returns>True if refresh successful, false if no refresh token available</returns>
    Task<bool> RefreshTokenAsync(Guid tenantId);

    /// <summary>
    /// Revoke OAuth token for a tenant
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <returns>True if revocation successful</returns>
    Task<bool> RevokeTokenAsync(Guid tenantId);

    /// <summary>
    /// Generate and store a CSRF state token
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <returns>CSRF state token</returns>
    string GenerateStateToken(Guid tenantId);

    /// <summary>
    /// Validate and extract tenant ID from state token
    /// </summary>
    /// <param name="state">State token to validate</param>
    /// <returns>Tenant ID if valid, null otherwise</returns>
    Guid? ValidateStateToken(string state);
}

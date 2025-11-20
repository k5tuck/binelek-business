using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Binah.Webhooks.Services.Interfaces;
using System.Security.Claims;

namespace Binah.Webhooks.Controllers;

/// <summary>
/// GitHub OAuth controller for authentication flow
/// </summary>
[ApiController]
[Route("api/github/oauth")]
[Authorize]
public class GitHubOAuthController : ControllerBase
{
    private readonly IGitHubAuthService _authService;
    private readonly IGitHubApiClient _apiClient;
    private readonly ILogger<GitHubOAuthController> _logger;

    public GitHubOAuthController(
        IGitHubAuthService authService,
        IGitHubApiClient apiClient,
        ILogger<GitHubOAuthController> logger)
    {
        _authService = authService;
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <summary>
    /// Initiate GitHub OAuth authorization flow
    /// </summary>
    /// <remarks>
    /// Redirects the user to GitHub's authorization page.
    /// After authorization, GitHub will redirect back to the callback endpoint.
    /// </remarks>
    /// <response code="302">Redirects to GitHub authorization page</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    [HttpGet("authorize")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Authorize()
    {
        try
        {
            var tenantId = GetTenantIdFromClaims();
            if (tenantId == null)
            {
                return Unauthorized(new { message = "Tenant ID not found in JWT claims" });
            }

            // Generate CSRF state token
            var state = _authService.GenerateStateToken(tenantId.Value);

            // Generate GitHub authorization URL
            var authUrl = _authService.GetAuthorizationUrl(tenantId.Value, state);

            _logger.LogInformation("Redirecting tenant {TenantId} to GitHub authorization", tenantId);

            return Redirect(authUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating GitHub OAuth authorization");
            return StatusCode(500, new { message = "Failed to initiate authorization", error = ex.Message });
        }
    }

    /// <summary>
    /// GitHub OAuth callback endpoint
    /// </summary>
    /// <param name="code">Authorization code from GitHub</param>
    /// <param name="state">CSRF state token</param>
    /// <remarks>
    /// This endpoint is called by GitHub after the user authorizes the application.
    /// It exchanges the authorization code for an access token and stores it.
    /// </remarks>
    /// <response code="200">Authorization successful, token stored</response>
    /// <response code="400">Invalid code or state parameter</response>
    /// <response code="401">Invalid or expired state token (CSRF protection)</response>
    [HttpGet("callback")]
    [AllowAnonymous] // GitHub will redirect here without JWT token
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string state)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return BadRequest(new { message = "Authorization code is required" });
            }

            if (string.IsNullOrWhiteSpace(state))
            {
                return BadRequest(new { message = "State parameter is required" });
            }

            // Exchange code for token (validates state internally)
            var tenantId = await _authService.ExchangeCodeForTokenAsync(code, state);

            _logger.LogInformation("Successfully completed OAuth flow for tenant {TenantId}", tenantId);

            // Return success page or redirect to frontend
            return Ok(new
            {
                message = "GitHub authorization successful",
                tenant_id = tenantId,
                next_steps = "You can now use GitHub API features. You may close this window."
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("CSRF"))
        {
            _logger.LogWarning("CSRF validation failed: {Message}", ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GitHub OAuth callback");
            return StatusCode(500, new { message = "Failed to complete authorization", error = ex.Message });
        }
    }

    /// <summary>
    /// Revoke GitHub OAuth token for current tenant
    /// </summary>
    /// <remarks>
    /// Revokes the OAuth token with GitHub and removes it from the database.
    /// </remarks>
    /// <response code="200">Token revoked successfully</response>
    /// <response code="404">No token found for tenant</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    [HttpDelete("revoke")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Revoke()
    {
        try
        {
            var tenantId = GetTenantIdFromClaims();
            if (tenantId == null)
            {
                return Unauthorized(new { message = "Tenant ID not found in JWT claims" });
            }

            var revoked = await _authService.RevokeTokenAsync(tenantId.Value);

            if (!revoked)
            {
                return NotFound(new { message = "No GitHub OAuth token found for this tenant" });
            }

            _logger.LogInformation("Revoked GitHub OAuth token for tenant {TenantId}", tenantId);

            return Ok(new { message = "GitHub OAuth token revoked successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking GitHub OAuth token");
            return StatusCode(500, new { message = "Failed to revoke token", error = ex.Message });
        }
    }

    /// <summary>
    /// Get GitHub OAuth status for current tenant
    /// </summary>
    /// <remarks>
    /// Check if the tenant has a valid GitHub OAuth token and can make API calls.
    /// </remarks>
    /// <response code="200">Returns OAuth status</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    [HttpGet("status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetStatus()
    {
        try
        {
            var tenantId = GetTenantIdFromClaims();
            if (tenantId == null)
            {
                return Unauthorized(new { message = "Tenant ID not found in JWT claims" });
            }

            // Try to initialize API client to check if token exists and is valid
            var initialized = await _apiClient.InitializeForTenantAsync(tenantId.Value);

            if (!initialized)
            {
                return Ok(new
                {
                    connected = false,
                    message = "No GitHub OAuth token found. Please authorize first.",
                    authorize_url = Url.Action(nameof(Authorize), "GitHubOAuth", null, Request.Scheme)
                });
            }

            // Get authenticated user to verify token is valid
            try
            {
                var user = await _apiClient.GetAuthenticatedUserAsync();

                return Ok(new
                {
                    connected = true,
                    github_user = new
                    {
                        login = user.Login,
                        name = user.Name,
                        email = user.Email,
                        avatar_url = user.AvatarUrl
                    }
                });
            }
            catch (Exception)
            {
                return Ok(new
                {
                    connected = false,
                    message = "GitHub OAuth token exists but may be invalid. Please re-authorize.",
                    authorize_url = Url.Action(nameof(Authorize), "GitHubOAuth", null, Request.Scheme)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking GitHub OAuth status");
            return StatusCode(500, new { message = "Failed to check status", error = ex.Message });
        }
    }

    /// <summary>
    /// Extract tenant ID from JWT claims
    /// </summary>
    private Guid? GetTenantIdFromClaims()
    {
        var tenantIdClaim = User.FindFirst("tenant_id")?.Value;
        if (Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            return tenantId;
        }

        // Try alternative claim name (some JWTs use tenantId instead of tenant_id)
        tenantIdClaim = User.FindFirst("tenantId")?.Value;
        if (Guid.TryParse(tenantIdClaim, out tenantId))
        {
            return tenantId;
        }

        return null;
    }
}

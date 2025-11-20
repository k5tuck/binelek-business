using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Binah.Webhooks.Services.Interfaces;
using Binah.Webhooks.Repositories.Interfaces;
using Binah.Webhooks.Models.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Binah.Webhooks.Services.Implementations;

/// <summary>
/// GitHub OAuth authentication service implementation
/// </summary>
public class GitHubAuthService : IGitHubAuthService
{
    private readonly IGitHubOAuthTokenRepository _tokenRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GitHubAuthService> _logger;
    private readonly HttpClient _httpClient;

    // In-memory state token storage (for CSRF protection)
    // In production, use distributed cache (Redis) for multi-instance deployments
    private static readonly ConcurrentDictionary<string, StateTokenData> _stateTokens = new();

    private string ClientId => _configuration["GitHub:ClientId"]
        ?? throw new InvalidOperationException("GitHub:ClientId not configured");

    private string ClientSecret => _configuration["GitHub:ClientSecret"]
        ?? throw new InvalidOperationException("GitHub:ClientSecret not configured");

    private string RedirectUri => _configuration["GitHub:RedirectUri"]
        ?? "http://localhost:8098/api/github/oauth/callback";

    private string Scopes => _configuration["GitHub:Scopes"] ?? "repo,user";

    public GitHubAuthService(
        IGitHubOAuthTokenRepository tokenRepository,
        IConfiguration configuration,
        ILogger<GitHubAuthService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _tokenRepository = tokenRepository;
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
    }

    /// <inheritdoc/>
    public string GetAuthorizationUrl(Guid tenantId, string state)
    {
        _logger.LogInformation("Generating GitHub OAuth authorization URL for tenant {TenantId}", tenantId);

        var authUrl = $"https://github.com/login/oauth/authorize" +
            $"?client_id={ClientId}" +
            $"&redirect_uri={Uri.EscapeDataString(RedirectUri)}" +
            $"&scope={Uri.EscapeDataString(Scopes)}" +
            $"&state={state}";

        return authUrl;
    }

    /// <inheritdoc/>
    public async Task<Guid> ExchangeCodeForTokenAsync(string code, string state)
    {
        try
        {
            // Validate state token (CSRF protection)
            var tenantId = ValidateStateToken(state);
            if (tenantId == null)
            {
                _logger.LogWarning("Invalid or expired state token: {State}", state);
                throw new InvalidOperationException("Invalid or expired state token. Possible CSRF attack.");
            }

            _logger.LogInformation("Exchanging authorization code for access token for tenant {TenantId}", tenantId);

            // Exchange code for token
            var tokenResponse = await ExchangeCodeWithGitHub(code);

            // Store token in database
            var oauthToken = new GitHubOAuthToken
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId.Value,
                AccessToken = tokenResponse.AccessToken,
                TokenType = tokenResponse.TokenType ?? "Bearer",
                Scope = tokenResponse.Scope,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = tokenResponse.ExpiresIn.HasValue
                    ? DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn.Value)
                    : null,
                RefreshToken = tokenResponse.RefreshToken
            };

            await _tokenRepository.UpsertAsync(oauthToken);

            // Remove state token after successful exchange
            _stateTokens.TryRemove(state, out _);

            _logger.LogInformation("Successfully stored OAuth token for tenant {TenantId}", tenantId);
            return tenantId.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exchanging authorization code for token");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> RefreshTokenAsync(Guid tenantId)
    {
        try
        {
            _logger.LogInformation("Refreshing OAuth token for tenant {TenantId}", tenantId);

            var existingToken = await _tokenRepository.GetByTenantAsync(tenantId);
            if (existingToken == null || string.IsNullOrEmpty(existingToken.RefreshToken))
            {
                _logger.LogWarning("No refresh token found for tenant {TenantId}", tenantId);
                return false;
            }

            // Note: GitHub OAuth tokens typically don't expire and don't have refresh tokens
            // This is here for future compatibility if GitHub adds token expiration
            _logger.LogWarning("GitHub OAuth tokens do not currently support refresh. Token refresh not needed.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token for tenant {TenantId}", tenantId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> RevokeTokenAsync(Guid tenantId)
    {
        try
        {
            _logger.LogInformation("Revoking OAuth token for tenant {TenantId}", tenantId);

            var token = await _tokenRepository.GetByTenantAsync(tenantId);
            if (token == null)
            {
                _logger.LogWarning("No token found to revoke for tenant {TenantId}", tenantId);
                return false;
            }

            // Revoke token with GitHub
            var requestData = new Dictionary<string, string>
            {
                { "access_token", token.AccessToken }
            };

            var request = new HttpRequestMessage(HttpMethod.Delete,
                $"https://api.github.com/applications/{ClientId}/token")
            {
                Content = new FormUrlEncodedContent(requestData)
            };

            // GitHub requires basic auth with client_id and client_secret
            var authBytes = Encoding.UTF8.GetBytes($"{ClientId}:{ClientSecret}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String(authBytes));

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                // Delete token from database
                await _tokenRepository.DeleteAsync(tenantId);
                _logger.LogInformation("Successfully revoked OAuth token for tenant {TenantId}", tenantId);
                return true;
            }
            else
            {
                _logger.LogWarning("Failed to revoke token with GitHub. Status: {StatusCode}", response.StatusCode);
                // Still delete from database even if GitHub revocation fails
                await _tokenRepository.DeleteAsync(tenantId);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking token for tenant {TenantId}", tenantId);
            throw;
        }
    }

    /// <inheritdoc/>
    public string GenerateStateToken(Guid tenantId)
    {
        // Generate cryptographically secure random state token
        var randomBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        var state = Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");

        // Store state with tenant ID and expiration (15 minutes)
        var stateData = new StateTokenData
        {
            TenantId = tenantId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15)
        };

        _stateTokens.TryAdd(state, stateData);

        // Clean up expired tokens (basic cleanup)
        CleanupExpiredStateTokens();

        _logger.LogDebug("Generated state token for tenant {TenantId}", tenantId);
        return state;
    }

    /// <inheritdoc/>
    public Guid? ValidateStateToken(string state)
    {
        if (string.IsNullOrWhiteSpace(state))
            return null;

        if (!_stateTokens.TryGetValue(state, out var stateData))
        {
            _logger.LogWarning("State token not found: {State}", state);
            return null;
        }

        if (DateTime.UtcNow > stateData.ExpiresAt)
        {
            _logger.LogWarning("State token expired: {State}", state);
            _stateTokens.TryRemove(state, out _);
            return null;
        }

        return stateData.TenantId;
    }

    /// <summary>
    /// Exchange authorization code with GitHub for access token
    /// </summary>
    private async Task<GitHubTokenResponse> ExchangeCodeWithGitHub(string code)
    {
        var requestData = new Dictionary<string, string>
        {
            { "client_id", ClientId },
            { "client_secret", ClientSecret },
            { "code", code },
            { "redirect_uri", RedirectUri }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://github.com/login/oauth/access_token")
        {
            Content = new FormUrlEncodedContent(requestData)
        };
        request.Headers.Add("Accept", "application/json");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<GitHubTokenResponse>(responseContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
        {
            throw new InvalidOperationException("Failed to obtain access token from GitHub");
        }

        return tokenResponse;
    }

    /// <summary>
    /// Clean up expired state tokens (basic implementation)
    /// In production, use scheduled background job
    /// </summary>
    private void CleanupExpiredStateTokens()
    {
        var now = DateTime.UtcNow;
        var expiredTokens = _stateTokens
            .Where(kvp => now > kvp.Value.ExpiresAt)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var token in expiredTokens)
        {
            _stateTokens.TryRemove(token, out _);
        }
    }

    /// <summary>
    /// State token data for CSRF protection
    /// </summary>
    private class StateTokenData
    {
        public Guid TenantId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    /// <summary>
    /// GitHub OAuth token response
    /// </summary>
    private class GitHubTokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string? TokenType { get; set; }
        public string? Scope { get; set; }
        public int? ExpiresIn { get; set; }
        public string? RefreshToken { get; set; }
    }
}

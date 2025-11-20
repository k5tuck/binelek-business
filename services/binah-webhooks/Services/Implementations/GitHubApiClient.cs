using Octokit;
using Binah.Webhooks.Services.Interfaces;
using Binah.Webhooks.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Binah.Webhooks.Services.Implementations;

/// <summary>
/// GitHub API client wrapper implementation using Octokit
/// </summary>
public class GitHubApiClient : IGitHubApiClient
{
    private readonly IGitHubOAuthTokenRepository _tokenRepository;
    private readonly ILogger<GitHubApiClient> _logger;
    private GitHubClient? _client;
    private Guid? _currentTenantId;

    public GitHubApiClient(
        IGitHubOAuthTokenRepository tokenRepository,
        ILogger<GitHubApiClient> logger)
    {
        _tokenRepository = tokenRepository;
        _logger = logger;
    }

    /// <inheritdoc/>
    public bool IsInitialized => _client != null;

    /// <inheritdoc/>
    public Guid? CurrentTenantId => _currentTenantId;

    /// <inheritdoc/>
    public async Task<bool> InitializeForTenantAsync(Guid tenantId)
    {
        try
        {
            _logger.LogInformation("Initializing GitHub API client for tenant {TenantId}", tenantId);

            // Get OAuth token for tenant
            var token = await _tokenRepository.GetByTenantAsync(tenantId);
            if (token == null)
            {
                _logger.LogWarning("No GitHub OAuth token found for tenant {TenantId}", tenantId);
                return false;
            }

            // Create GitHub client with OAuth token
            _client = new GitHubClient(new ProductHeaderValue("Binelek"))
            {
                Credentials = new Credentials(token.AccessToken)
            };

            _currentTenantId = tenantId;

            _logger.LogInformation("GitHub API client initialized successfully for tenant {TenantId}", tenantId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing GitHub API client for tenant {TenantId}", tenantId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<User> GetAuthenticatedUserAsync()
    {
        EnsureInitialized();

        try
        {
            _logger.LogDebug("Getting authenticated user for tenant {TenantId}", _currentTenantId);
            var user = await _client!.User.Current();
            _logger.LogInformation("Retrieved authenticated user: {Login}", user.Login);
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting authenticated user for tenant {TenantId}", _currentTenantId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Repository> GetRepositoryAsync(string owner, string name)
    {
        EnsureInitialized();

        if (string.IsNullOrWhiteSpace(owner))
            throw new ArgumentException("Repository owner cannot be empty", nameof(owner));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Repository name cannot be empty", nameof(name));

        try
        {
            _logger.LogDebug("Getting repository {Owner}/{Name} for tenant {TenantId}", owner, name, _currentTenantId);
            var repository = await _client!.Repository.Get(owner, name);
            _logger.LogInformation("Retrieved repository: {FullName}", repository.FullName);
            return repository;
        }
        catch (NotFoundException)
        {
            _logger.LogWarning("Repository {Owner}/{Name} not found", owner, name);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting repository {Owner}/{Name}", owner, name);
            throw;
        }
    }

    /// <summary>
    /// Ensure the client is initialized before making API calls
    /// </summary>
    private void EnsureInitialized()
    {
        if (_client == null)
        {
            throw new InvalidOperationException(
                "GitHub API client is not initialized. Call InitializeForTenantAsync first.");
        }
    }
}

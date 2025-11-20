using Octokit;
using Binah.Webhooks.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Binah.Webhooks.Services.Implementations;

/// <summary>
/// Service for GitHub branch operations
/// </summary>
public class GitHubBranchService : IGitHubBranchService
{
    private readonly IGitHubApiClient _apiClient;
    private readonly ILogger<GitHubBranchService> _logger;

    public GitHubBranchService(
        IGitHubApiClient apiClient,
        ILogger<GitHubBranchService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Branch> GetBranchAsync(string owner, string repo, string branchName)
    {
        EnsureClientInitialized();
        ValidateParameters(owner, repo, branchName);

        try
        {
            _logger.LogDebug("Getting branch {BranchName} from {Owner}/{Repo}", branchName, owner, repo);

            var client = GetGitHubClient();
            var branch = await client.Repository.Branch.Get(owner, repo, branchName);

            _logger.LogInformation("Successfully retrieved branch {BranchName} from {Owner}/{Repo}", branchName, owner, repo);
            return branch;
        }
        catch (NotFoundException)
        {
            _logger.LogWarning("Branch {BranchName} not found in {Owner}/{Repo}", branchName, owner, repo);
            throw new InvalidOperationException($"Branch '{branchName}' not found in repository '{owner}/{repo}'");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting branch {BranchName} from {Owner}/{Repo}", branchName, owner, repo);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Reference> CreateBranchAsync(string owner, string repo, string branchName, string fromSha)
    {
        EnsureClientInitialized();
        ValidateParameters(owner, repo, branchName);

        if (string.IsNullOrWhiteSpace(fromSha))
            throw new ArgumentException("Source SHA cannot be empty", nameof(fromSha));

        try
        {
            _logger.LogDebug("Creating branch {BranchName} from SHA {Sha} in {Owner}/{Repo}", branchName, fromSha, owner, repo);

            // Check if branch already exists
            if (await BranchExistsAsync(owner, repo, branchName))
            {
                _logger.LogWarning("Branch {BranchName} already exists in {Owner}/{Repo}", branchName, owner, repo);
                throw new InvalidOperationException($"Branch '{branchName}' already exists in repository '{owner}/{repo}'");
            }

            var client = GetGitHubClient();
            var reference = new NewReference($"refs/heads/{branchName}", fromSha);
            var createdRef = await client.Git.Reference.Create(owner, repo, reference);

            _logger.LogInformation("Successfully created branch {BranchName} from SHA {Sha} in {Owner}/{Repo}", branchName, fromSha, owner, repo);
            return createdRef;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Error creating branch {BranchName} from SHA {Sha} in {Owner}/{Repo}", branchName, fromSha, owner, repo);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task DeleteBranchAsync(string owner, string repo, string branchName)
    {
        EnsureClientInitialized();
        ValidateParameters(owner, repo, branchName);

        try
        {
            _logger.LogDebug("Deleting branch {BranchName} from {Owner}/{Repo}", branchName, owner, repo);

            // Check if branch exists
            if (!await BranchExistsAsync(owner, repo, branchName))
            {
                _logger.LogWarning("Branch {BranchName} does not exist in {Owner}/{Repo}", branchName, owner, repo);
                throw new InvalidOperationException($"Branch '{branchName}' does not exist in repository '{owner}/{repo}'");
            }

            var client = GetGitHubClient();
            await client.Git.Reference.Delete(owner, repo, $"heads/{branchName}");

            _logger.LogInformation("Successfully deleted branch {BranchName} from {Owner}/{Repo}", branchName, owner, repo);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Error deleting branch {BranchName} from {Owner}/{Repo}", branchName, owner, repo);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> BranchExistsAsync(string owner, string repo, string branchName)
    {
        EnsureClientInitialized();
        ValidateParameters(owner, repo, branchName);

        try
        {
            _logger.LogDebug("Checking if branch {BranchName} exists in {Owner}/{Repo}", branchName, owner, repo);

            var client = GetGitHubClient();
            await client.Repository.Branch.Get(owner, repo, branchName);

            _logger.LogDebug("Branch {BranchName} exists in {Owner}/{Repo}", branchName, owner, repo);
            return true;
        }
        catch (NotFoundException)
        {
            _logger.LogDebug("Branch {BranchName} does not exist in {Owner}/{Repo}", branchName, owner, repo);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if branch {BranchName} exists in {Owner}/{Repo}", branchName, owner, repo);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<string> GetDefaultBranchAsync(string owner, string repo)
    {
        EnsureClientInitialized();
        ValidateParameters(owner, repo);

        try
        {
            _logger.LogDebug("Getting default branch for {Owner}/{Repo}", owner, repo);

            var repository = await _apiClient.GetRepositoryAsync(owner, repo);
            var defaultBranch = repository.DefaultBranch;

            _logger.LogInformation("Default branch for {Owner}/{Repo} is {DefaultBranch}", owner, repo, defaultBranch);
            return defaultBranch;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting default branch for {Owner}/{Repo}", owner, repo);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<string> GetBranchHeadShaAsync(string owner, string repo, string branchName)
    {
        EnsureClientInitialized();
        ValidateParameters(owner, repo, branchName);

        try
        {
            _logger.LogDebug("Getting HEAD SHA for branch {BranchName} in {Owner}/{Repo}", branchName, owner, repo);

            var branch = await GetBranchAsync(owner, repo, branchName);
            var sha = branch.Commit.Sha;

            _logger.LogInformation("HEAD SHA for branch {BranchName} in {Owner}/{Repo} is {Sha}", branchName, owner, repo, sha);
            return sha;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting HEAD SHA for branch {BranchName} in {Owner}/{Repo}", branchName, owner, repo);
            throw;
        }
    }

    /// <summary>
    /// Ensure the GitHub API client is initialized
    /// </summary>
    private void EnsureClientInitialized()
    {
        if (!_apiClient.IsInitialized)
        {
            throw new InvalidOperationException(
                "GitHub API client is not initialized. Call InitializeForTenantAsync first.");
        }
    }

    /// <summary>
    /// Get the underlying GitHubClient using reflection (since it's not exposed in the interface)
    /// </summary>
    private GitHubClient GetGitHubClient()
    {
        // Since the GitHubApiClient doesn't expose the GitHubClient directly,
        // we need to access it via reflection or create our own client
        // For now, we'll create a client using the same pattern as GitHubApiClient

        // Alternative: We could modify IGitHubApiClient to expose the GitHubClient
        // or add specific methods for branch/commit operations

        var clientType = _apiClient.GetType();
        var clientField = clientType.GetField("_client",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (clientField == null)
            throw new InvalidOperationException("Unable to access GitHubClient from API client");

        var client = clientField.GetValue(_apiClient) as GitHubClient;
        if (client == null)
            throw new InvalidOperationException("GitHubClient is not initialized");

        return client;
    }

    /// <summary>
    /// Validate common parameters
    /// </summary>
    private void ValidateParameters(string owner, string repo, string? branchName = null)
    {
        if (string.IsNullOrWhiteSpace(owner))
            throw new ArgumentException("Repository owner cannot be empty", nameof(owner));

        if (string.IsNullOrWhiteSpace(repo))
            throw new ArgumentException("Repository name cannot be empty", nameof(repo));

        if (branchName != null && string.IsNullOrWhiteSpace(branchName))
            throw new ArgumentException("Branch name cannot be empty", nameof(branchName));
    }
}

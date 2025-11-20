using Octokit;
using Binah.Webhooks.Services.Interfaces;
using Binah.Webhooks.Models.DTOs.GitHub;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Binah.Webhooks.Services.Implementations;

/// <summary>
/// Service for GitHub commit operations
/// </summary>
public class GitHubCommitService : IGitHubCommitService
{
    private readonly IGitHubApiClient _apiClient;
    private readonly IGitHubBranchService _branchService;
    private readonly ILogger<GitHubCommitService> _logger;

    public GitHubCommitService(
        IGitHubApiClient apiClient,
        IGitHubBranchService branchService,
        ILogger<GitHubCommitService> logger)
    {
        _apiClient = apiClient;
        _branchService = branchService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<string> CreateCommitAsync(
        string owner,
        string repo,
        string branchName,
        string message,
        List<GitHubFileChange> files)
    {
        EnsureClientInitialized();
        ValidateParameters(owner, repo, branchName, message);

        if (files == null || files.Count == 0)
            throw new ArgumentException("At least one file change is required", nameof(files));

        try
        {
            _logger.LogDebug("Creating commit with {FileCount} file(s) on branch {BranchName} in {Owner}/{Repo}",
                files.Count, branchName, owner, repo);

            var client = GetGitHubClient();

            // Step 1: Get reference to branch HEAD
            var branchHeadSha = await _branchService.GetBranchHeadShaAsync(owner, repo, branchName);
            _logger.LogDebug("Branch HEAD SHA: {Sha}", branchHeadSha);

            // Step 2: Get current tree SHA
            var commit = await client.Git.Commit.Get(owner, repo, branchHeadSha);
            var baseTreeSha = commit.Tree.Sha;
            _logger.LogDebug("Base tree SHA: {Sha}", baseTreeSha);

            // Step 3: Create blobs for all file contents
            var newTree = new NewTree { BaseTree = baseTreeSha };

            foreach (var file in files)
            {
                await ProcessFileChange(client, owner, repo, file, newTree);
            }

            // Step 4: Create new tree with all changes
            var tree = await client.Git.Tree.Create(owner, repo, newTree);
            _logger.LogDebug("Created new tree: {Sha}", tree.Sha);

            // Step 5: Create commit pointing to new tree
            var newCommit = new NewCommit(message, tree.Sha, branchHeadSha);
            var createdCommit = await client.Git.Commit.Create(owner, repo, newCommit);
            _logger.LogDebug("Created commit: {Sha}", createdCommit.Sha);

            // Step 6: Update branch reference to new commit SHA
            await client.Git.Reference.Update(owner, repo, $"heads/{branchName}",
                new ReferenceUpdate(createdCommit.Sha));

            _logger.LogInformation("Successfully created commit {Sha} with {FileCount} file(s) on branch {BranchName} in {Owner}/{Repo}",
                createdCommit.Sha, files.Count, branchName, owner, repo);

            return createdCommit.Sha;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating commit on branch {BranchName} in {Owner}/{Repo}", branchName, owner, repo);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<GitHubCommit> GetCommitAsync(string owner, string repo, string sha)
    {
        EnsureClientInitialized();
        ValidateParameters(owner, repo);

        if (string.IsNullOrWhiteSpace(sha))
            throw new ArgumentException("Commit SHA cannot be empty", nameof(sha));

        try
        {
            _logger.LogDebug("Getting commit {Sha} from {Owner}/{Repo}", sha, owner, repo);

            var client = GetGitHubClient();
            var commit = await client.Git.Commit.Get(owner, repo, sha);

            _logger.LogInformation("Successfully retrieved commit {Sha} from {Owner}/{Repo}", sha, owner, repo);
            return commit;
        }
        catch (NotFoundException)
        {
            _logger.LogWarning("Commit {Sha} not found in {Owner}/{Repo}", sha, owner, repo);
            throw new InvalidOperationException($"Commit '{sha}' not found in repository '{owner}/{repo}'");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting commit {Sha} from {Owner}/{Repo}", sha, owner, repo);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<RepositoryContentChangeSet> UpdateFileAsync(
        string owner,
        string repo,
        string path,
        string content,
        string message,
        string branchName,
        string sha)
    {
        EnsureClientInitialized();
        ValidateParameters(owner, repo, path, message);

        if (string.IsNullOrWhiteSpace(sha))
            throw new ArgumentException("File SHA is required for update", nameof(sha));

        try
        {
            _logger.LogDebug("Updating file {Path} on branch {BranchName} in {Owner}/{Repo}", path, branchName, owner, repo);

            var client = GetGitHubClient();
            var updateRequest = new UpdateFileRequest(message, content, sha, branchName);
            var result = await client.Repository.Content.UpdateFile(owner, repo, path, updateRequest);

            _logger.LogInformation("Successfully updated file {Path} on branch {BranchName} in {Owner}/{Repo}", path, branchName, owner, repo);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating file {Path} on branch {BranchName} in {Owner}/{Repo}", path, branchName, owner, repo);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<RepositoryContentChangeSet> CreateFileAsync(
        string owner,
        string repo,
        string path,
        string content,
        string message,
        string branchName)
    {
        EnsureClientInitialized();
        ValidateParameters(owner, repo, path, message);

        try
        {
            _logger.LogDebug("Creating file {Path} on branch {BranchName} in {Owner}/{Repo}", path, branchName, owner, repo);

            var client = GetGitHubClient();
            var createRequest = new CreateFileRequest(message, content, branchName);
            var result = await client.Repository.Content.CreateFile(owner, repo, path, createRequest);

            _logger.LogInformation("Successfully created file {Path} on branch {BranchName} in {Owner}/{Repo}", path, branchName, owner, repo);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating file {Path} on branch {BranchName} in {Owner}/{Repo}", path, branchName, owner, repo);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task DeleteFileAsync(
        string owner,
        string repo,
        string path,
        string message,
        string branchName,
        string sha)
    {
        EnsureClientInitialized();
        ValidateParameters(owner, repo, path, message);

        if (string.IsNullOrWhiteSpace(sha))
            throw new ArgumentException("File SHA is required for delete", nameof(sha));

        try
        {
            _logger.LogDebug("Deleting file {Path} on branch {BranchName} in {Owner}/{Repo}", path, branchName, owner, repo);

            var client = GetGitHubClient();
            var deleteRequest = new DeleteFileRequest(message, sha, branchName);
            await client.Repository.Content.DeleteFile(owner, repo, path, deleteRequest);

            _logger.LogInformation("Successfully deleted file {Path} on branch {BranchName} in {Owner}/{Repo}", path, branchName, owner, repo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {Path} on branch {BranchName} in {Owner}/{Repo}", path, branchName, owner, repo);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<RepositoryContent> GetFileAsync(
        string owner,
        string repo,
        string path,
        string? branchName = null)
    {
        EnsureClientInitialized();
        ValidateParameters(owner, repo, path);

        try
        {
            _logger.LogDebug("Getting file {Path} from {Owner}/{Repo} (branch: {BranchName})",
                path, owner, repo, branchName ?? "default");

            var client = GetGitHubClient();

            // Get file contents
            var contents = branchName == null
                ? await client.Repository.Content.GetAllContents(owner, repo, path)
                : await client.Repository.Content.GetAllContentsByRef(owner, repo, path, branchName);

            if (contents == null || contents.Count == 0)
            {
                throw new InvalidOperationException($"File '{path}' not found in repository '{owner}/{repo}'");
            }

            var file = contents[0];

            _logger.LogInformation("Successfully retrieved file {Path} from {Owner}/{Repo}", path, owner, repo);
            return file;
        }
        catch (NotFoundException)
        {
            _logger.LogWarning("File {Path} not found in {Owner}/{Repo}", path, owner, repo);
            throw new InvalidOperationException($"File '{path}' not found in repository '{owner}/{repo}'");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file {Path} from {Owner}/{Repo}", path, owner, repo);
            throw;
        }
    }

    /// <summary>
    /// Process a file change and add it to the tree
    /// </summary>
    private async Task ProcessFileChange(
        GitHubClient client,
        string owner,
        string repo,
        GitHubFileChange file,
        NewTree tree)
    {
        switch (file.Mode)
        {
            case GitHubFileChangeMode.Add:
            case GitHubFileChangeMode.Update:
                // Create blob for file content
                var blob = new NewBlob
                {
                    Content = file.Content,
                    Encoding = EncodingType.Utf8
                };
                var createdBlob = await client.Git.Blob.Create(owner, repo, blob);

                _logger.LogDebug("Created blob {Sha} for file {Path}", createdBlob.Sha, file.Path);

                // Add to tree
                tree.Tree.Add(new NewTreeItem
                {
                    Path = file.Path,
                    Mode = "100644", // Regular file
                    Type = TreeType.Blob,
                    Sha = createdBlob.Sha
                });
                break;

            case GitHubFileChangeMode.Delete:
                // Add null SHA to delete file
                tree.Tree.Add(new NewTreeItem
                {
                    Path = file.Path,
                    Mode = "100644",
                    Type = TreeType.Blob,
                    Sha = null // Null SHA means delete
                });
                _logger.LogDebug("Marked file {Path} for deletion", file.Path);
                break;

            default:
                throw new InvalidOperationException($"Unsupported file change mode: {file.Mode}");
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
    /// Get the underlying GitHubClient using reflection
    /// </summary>
    private GitHubClient GetGitHubClient()
    {
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
    private void ValidateParameters(string owner, string repo, string? path = null, string? message = null)
    {
        if (string.IsNullOrWhiteSpace(owner))
            throw new ArgumentException("Repository owner cannot be empty", nameof(owner));

        if (string.IsNullOrWhiteSpace(repo))
            throw new ArgumentException("Repository name cannot be empty", nameof(repo));

        if (path != null && string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("File path cannot be empty", nameof(path));

        if (message != null && string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Commit message cannot be empty", nameof(message));
    }
}

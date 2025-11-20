using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Octokit;

namespace Binah.Webhooks.Tests.Helpers;

/// <summary>
/// Helper utilities for GitHub integration tests
/// Provides methods for creating test branches, files, and cleanup
/// </summary>
public class GitHubTestHelper : IDisposable
{
    private readonly GitHubClient _client;
    private readonly string _testRepository;
    private readonly string _testRepositoryOwner;
    private readonly List<string> _createdBranches;
    private readonly List<string> _createdPullRequests;
    private bool _disposed;

    /// <summary>
    /// Initialize GitHub test helper
    /// </summary>
    /// <param name="accessToken">GitHub personal access token (from GITHUB_TEST_TOKEN env var)</param>
    /// <param name="repository">Test repository in format "owner/repo" (from GITHUB_TEST_REPO env var)</param>
    public GitHubTestHelper(string accessToken, string repository)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new ArgumentException("Access token cannot be empty", nameof(accessToken));

        if (string.IsNullOrWhiteSpace(repository))
            throw new ArgumentException("Repository cannot be empty", nameof(repository));

        var repoParts = repository.Split('/');
        if (repoParts.Length != 2)
            throw new ArgumentException("Repository must be in format 'owner/repo'", nameof(repository));

        _testRepositoryOwner = repoParts[0];
        _testRepository = repoParts[1];

        _client = new GitHubClient(new ProductHeaderValue("Binelek-IntegrationTests"))
        {
            Credentials = new Credentials(accessToken)
        };

        _createdBranches = new List<string>();
        _createdPullRequests = new List<string>();
    }

    /// <summary>
    /// Create a unique test branch with specified prefix
    /// Branch name format: {prefix}-test-{timestamp}
    /// </summary>
    /// <param name="prefix">Branch name prefix (e.g., "feature", "bugfix")</param>
    /// <param name="baseBranch">Base branch to create from (defaults to repository default branch)</param>
    /// <returns>Created branch name</returns>
    public async Task<string> CreateTestBranchAsync(string prefix = "test", string? baseBranch = null)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var branchName = $"{prefix}-test-{timestamp}";

        try
        {
            // Get repository to find default branch
            var repo = await _client.Repository.Get(_testRepositoryOwner, _testRepository);
            var baseRef = baseBranch ?? repo.DefaultBranch;

            // Get the SHA of the base branch
            var baseReference = await _client.Git.Reference.Get(
                _testRepositoryOwner,
                _testRepository,
                $"heads/{baseRef}");

            // Create new branch
            var newReference = new NewReference($"refs/heads/{branchName}", baseReference.Object.Sha);
            await _client.Git.Reference.Create(_testRepositoryOwner, _testRepository, newReference);

            _createdBranches.Add(branchName);
            return branchName;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to create test branch '{branchName}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Create a test file in the repository on specified branch
    /// </summary>
    /// <param name="branchName">Branch to create file on</param>
    /// <param name="filePath">Path for the file (e.g., "test/sample.txt")</param>
    /// <param name="content">File content</param>
    /// <param name="commitMessage">Commit message (optional)</param>
    /// <returns>SHA of the created commit</returns>
    public async Task<string> CreateTestFileAsync(
        string branchName,
        string filePath,
        string content,
        string? commitMessage = null)
    {
        commitMessage ??= $"test: Add {filePath}";

        try
        {
            // Check if file already exists
            RepositoryContent? existingFile = null;
            try
            {
                var files = await _client.Repository.Content.GetAllContentsByRef(
                    _testRepositoryOwner,
                    _testRepository,
                    filePath,
                    branchName);
                existingFile = files.FirstOrDefault();
            }
            catch (NotFoundException)
            {
                // File doesn't exist, which is fine
            }

            if (existingFile != null)
            {
                // Update existing file
                var updateRequest = new UpdateFileRequest(commitMessage, content, existingFile.Sha, branchName);
                var result = await _client.Repository.Content.UpdateFile(
                    _testRepositoryOwner,
                    _testRepository,
                    filePath,
                    updateRequest);
                return result.Commit.Sha;
            }
            else
            {
                // Create new file
                var createRequest = new CreateFileRequest(commitMessage, content, branchName);
                var result = await _client.Repository.Content.CreateFile(
                    _testRepositoryOwner,
                    _testRepository,
                    filePath,
                    createRequest);
                return result.Commit.Sha;
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to create file '{filePath}' on branch '{branchName}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Create a test pull request
    /// </summary>
    /// <param name="headBranch">Source branch (must exist)</param>
    /// <param name="baseBranch">Target branch (optional, defaults to repo default)</param>
    /// <param name="title">PR title</param>
    /// <param name="body">PR description</param>
    /// <returns>Pull request number</returns>
    public async Task<int> CreateTestPullRequestAsync(
        string headBranch,
        string? baseBranch = null,
        string? title = null,
        string? body = null)
    {
        try
        {
            var repo = await _client.Repository.Get(_testRepositoryOwner, _testRepository);
            var targetBranch = baseBranch ?? repo.DefaultBranch;

            title ??= $"Test PR from {headBranch}";
            body ??= "Automated test pull request - safe to close";

            var newPr = new NewPullRequest(title, headBranch, targetBranch)
            {
                Body = body
            };

            var pr = await _client.PullRequest.Create(_testRepositoryOwner, _testRepository, newPr);
            _createdPullRequests.Add(pr.Number.ToString());

            return pr.Number;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to create pull request from '{headBranch}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Assert that a pull request exists with expected properties
    /// </summary>
    /// <param name="prNumber">Pull request number to check</param>
    /// <param name="expectedTitle">Expected title (optional)</param>
    /// <param name="expectedState">Expected state (optional, e.g., "open", "closed")</param>
    /// <returns>Pull request details if assertions pass</returns>
    public async Task<PullRequest> AssertPullRequestExistsAsync(
        int prNumber,
        string? expectedTitle = null,
        ItemStateFilter? expectedState = null)
    {
        try
        {
            var pr = await _client.PullRequest.Get(_testRepositoryOwner, _testRepository, prNumber);

            if (expectedTitle != null && pr.Title != expectedTitle)
            {
                throw new Exception($"PR #{prNumber} title mismatch. Expected: '{expectedTitle}', Actual: '{pr.Title}'");
            }

            if (expectedState.HasValue)
            {
                var actualState = pr.State.Value == ItemState.Open ? ItemStateFilter.Open : ItemStateFilter.Closed;
                if (actualState != expectedState.Value)
                {
                    throw new Exception($"PR #{prNumber} state mismatch. Expected: '{expectedState}', Actual: '{actualState}'");
                }
            }

            return pr;
        }
        catch (NotFoundException)
        {
            throw new Exception($"Pull request #{prNumber} does not exist in {_testRepositoryOwner}/{_testRepository}");
        }
    }

    /// <summary>
    /// Assert that a branch exists
    /// </summary>
    /// <param name="branchName">Branch name to check</param>
    /// <returns>Branch reference if exists</returns>
    public async Task<Reference> AssertBranchExistsAsync(string branchName)
    {
        try
        {
            var branch = await _client.Git.Reference.Get(
                _testRepositoryOwner,
                _testRepository,
                $"heads/{branchName}");
            return branch;
        }
        catch (NotFoundException)
        {
            throw new Exception($"Branch '{branchName}' does not exist in {_testRepositoryOwner}/{_testRepository}");
        }
    }

    /// <summary>
    /// Close a test pull request
    /// </summary>
    /// <param name="prNumber">Pull request number</param>
    public async Task ClosePullRequestAsync(int prNumber)
    {
        try
        {
            var update = new PullRequestUpdate { State = ItemState.Closed };
            await _client.PullRequest.Update(_testRepositoryOwner, _testRepository, prNumber, update);
        }
        catch (Exception ex)
        {
            // Log but don't throw - this is cleanup
            Console.WriteLine($"Warning: Failed to close PR #{prNumber}: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete a test branch
    /// </summary>
    /// <param name="branchName">Branch name to delete</param>
    public async Task DeleteBranchAsync(string branchName)
    {
        try
        {
            await _client.Git.Reference.Delete(_testRepositoryOwner, _testRepository, $"heads/{branchName}");
        }
        catch (Exception ex)
        {
            // Log but don't throw - this is cleanup
            Console.WriteLine($"Warning: Failed to delete branch '{branchName}': {ex.Message}");
        }
    }

    /// <summary>
    /// Clean up all test branches created during this test session
    /// Called automatically on Dispose()
    /// </summary>
    public async Task CleanupTestBranchesAsync()
    {
        foreach (var branch in _createdBranches.ToList())
        {
            await DeleteBranchAsync(branch);
            _createdBranches.Remove(branch);
        }
    }

    /// <summary>
    /// Clean up all test pull requests created during this test session
    /// </summary>
    public async Task CleanupTestPullRequestsAsync()
    {
        foreach (var prNumberStr in _createdPullRequests.ToList())
        {
            if (int.TryParse(prNumberStr, out var prNumber))
            {
                await ClosePullRequestAsync(prNumber);
                _createdPullRequests.Remove(prNumberStr);
            }
        }
    }

    /// <summary>
    /// Get the authenticated user (useful for verifying token is valid)
    /// </summary>
    public async Task<User> GetAuthenticatedUserAsync()
    {
        return await _client.User.Current();
    }

    /// <summary>
    /// Get repository information
    /// </summary>
    public async Task<Repository> GetRepositoryAsync()
    {
        return await _client.Repository.Get(_testRepositoryOwner, _testRepository);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Async cleanup - fire and forget
                Task.Run(async () =>
                {
                    await CleanupTestPullRequestsAsync();
                    await CleanupTestBranchesAsync();
                }).Wait(TimeSpan.FromSeconds(30));
            }

            _disposed = true;
        }
    }
}

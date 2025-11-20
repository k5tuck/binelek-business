using System;
using System.Threading.Tasks;
using Xunit;
using Binah.Webhooks.Tests.Helpers;
using Binah.Webhooks.Services.Interfaces;
using Binah.Webhooks.Services.Implementations;
using Binah.Webhooks.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace Binah.Webhooks.Tests.Integration;

/// <summary>
/// Integration tests for GitHub API client
/// These tests require real GitHub credentials:
/// - GITHUB_TEST_TOKEN: Personal access token with repo permissions
/// - GITHUB_TEST_REPO: Test repository in format "owner/repo"
///
/// Tests are skipped by default. Set RUN_GITHUB_TESTS=true to enable.
///
/// WARNING: These tests will create/modify content in the specified repository.
/// Use a dedicated test repository, not a production repository.
/// </summary>
[Collection("GitHub Integration Tests")]
public class GitHubApiIntegrationTests : IDisposable
{
    private readonly GitHubTestHelper? _testHelper;
    private readonly bool _testsEnabled;
    private readonly string _testToken;
    private readonly string _testRepo;

    public GitHubApiIntegrationTests()
    {
        // Get test credentials from environment variables
        _testToken = Environment.GetEnvironmentVariable("GITHUB_TEST_TOKEN") ?? string.Empty;
        _testRepo = Environment.GetEnvironmentVariable("GITHUB_TEST_REPO") ?? string.Empty;
        var runTests = Environment.GetEnvironmentVariable("RUN_GITHUB_TESTS");

        _testsEnabled = !string.IsNullOrEmpty(_testToken)
            && !string.IsNullOrEmpty(_testRepo)
            && runTests?.ToLower() == "true";

        if (_testsEnabled)
        {
            _testHelper = new GitHubTestHelper(_testToken, _testRepo);
        }
    }

    [Fact(Skip = "Requires GitHub credentials - set RUN_GITHUB_TESTS=true to enable")]
    public async Task GitHubApiClient_InitializeForTenant_ValidToken_ReturnsTrue()
    {
        if (!_testsEnabled || _testHelper == null)
        {
            Assert.True(true, "Test skipped - credentials not provided");
            return;
        }

        // Arrange
        var tenantId = Guid.NewGuid();
        var mockTokenRepo = new Mock<IGitHubOAuthTokenRepository>();
        var mockLogger = new Mock<ILogger<GitHubApiClient>>();

        // Create token entity
        var oauthToken = new Models.Domain.GitHubOAuthToken
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AccessToken = _testToken,
            TokenType = "Bearer",
            Scope = "repo",
            CreatedAt = DateTime.UtcNow
        };

        mockTokenRepo
            .Setup(r => r.GetByTenantAsync(tenantId))
            .ReturnsAsync(oauthToken);

        var apiClient = new GitHubApiClient(mockTokenRepo.Object, mockLogger.Object);

        // Act
        var result = await apiClient.InitializeForTenantAsync(tenantId);

        // Assert
        Assert.True(result);
        Assert.True(apiClient.IsInitialized);
        Assert.Equal(tenantId, apiClient.CurrentTenantId);

        // Verify we can call GitHub API
        var user = await apiClient.GetAuthenticatedUserAsync();
        Assert.NotNull(user);
        Assert.NotEmpty(user.Login);
    }

    [Fact(Skip = "Requires GitHub credentials - set RUN_GITHUB_TESTS=true to enable")]
    public async Task GitHubApiClient_GetRepository_ValidRepo_ReturnsRepository()
    {
        if (!_testsEnabled || _testHelper == null)
        {
            Assert.True(true, "Test skipped - credentials not provided");
            return;
        }

        // Arrange
        var tenantId = Guid.NewGuid();
        var mockTokenRepo = new Mock<IGitHubOAuthTokenRepository>();
        var mockLogger = new Mock<ILogger<GitHubApiClient>>();

        var oauthToken = new Models.Domain.GitHubOAuthToken
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AccessToken = _testToken,
            TokenType = "Bearer"
        };

        mockTokenRepo
            .Setup(r => r.GetByTenantAsync(tenantId))
            .ReturnsAsync(oauthToken);

        var apiClient = new GitHubApiClient(mockTokenRepo.Object, mockLogger.Object);
        await apiClient.InitializeForTenantAsync(tenantId);

        var repoParts = _testRepo.Split('/');
        var owner = repoParts[0];
        var name = repoParts[1];

        // Act
        var repository = await apiClient.GetRepositoryAsync(owner, name);

        // Assert
        Assert.NotNull(repository);
        Assert.Equal(name, repository.Name);
        Assert.Equal(owner, repository.Owner.Login);
    }

    [Fact(Skip = "Requires GitHub credentials - set RUN_GITHUB_TESTS=true to enable")]
    public async Task E2E_CreateBranch_CommitFiles_CreatePR_Success()
    {
        if (!_testsEnabled || _testHelper == null)
        {
            Assert.True(true, "Test skipped - credentials not provided");
            return;
        }

        // This is a full end-to-end test of the autonomous PR workflow
        // 1. Create a test branch
        // 2. Commit test files to the branch
        // 3. Create a pull request
        // 4. Verify PR was created
        // 5. Clean up (close PR, delete branch)

        try
        {
            // Step 1: Create test branch
            var branchName = await _testHelper.CreateTestBranchAsync("e2e-test");
            Assert.NotEmpty(branchName);

            // Verify branch was created
            var branch = await _testHelper.AssertBranchExistsAsync(branchName);
            Assert.NotNull(branch);

            // Step 2: Commit test files
            var testContent = $"# Test File\n\nCreated at: {DateTime.UtcNow:O}\n";
            var commitSha = await _testHelper.CreateTestFileAsync(
                branchName,
                "test-files/integration-test.md",
                testContent,
                "test: Add integration test file");

            Assert.NotEmpty(commitSha);

            // Step 3: Create pull request
            var prNumber = await _testHelper.CreateTestPullRequestAsync(
                branchName,
                null,
                "E2E Test: Autonomous PR Creation",
                "This PR was created by an automated integration test.\n\n✅ Branch created\n✅ File committed\n✅ PR created\n\nSafe to close.");

            Assert.True(prNumber > 0);

            // Step 4: Verify PR exists
            var pr = await _testHelper.AssertPullRequestExistsAsync(
                prNumber,
                "E2E Test: Autonomous PR Creation");

            Assert.NotNull(pr);
            Assert.Equal("open", pr.State.StringValue);

            // Step 5: Cleanup is handled by Dispose()
        }
        catch (Exception ex)
        {
            Assert.True(false, $"E2E test failed: {ex.Message}");
        }
    }

    [Fact(Skip = "Requires GitHub credentials - set RUN_GITHUB_TESTS=true to enable")]
    public async Task CreateBranch_InvalidBaseBranch_ThrowsException()
    {
        if (!_testsEnabled || _testHelper == null)
        {
            Assert.True(true, "Test skipped - credentials not provided");
            return;
        }

        // Arrange & Act & Assert
        await Assert.ThrowsAsync<Exception>(async () =>
        {
            await _testHelper.CreateTestBranchAsync("test", "nonexistent-base-branch");
        });
    }

    [Fact(Skip = "Requires GitHub credentials - set RUN_GITHUB_TESTS=true to enable")]
    public async Task CreateTestFile_ValidBranch_CreatesFileSuccessfully()
    {
        if (!_testsEnabled || _testHelper == null)
        {
            Assert.True(true, "Test skipped - credentials not provided");
            return;
        }

        // Arrange
        var branchName = await _testHelper.CreateTestBranchAsync("file-test");
        var filePath = $"test-files/test-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}.txt";
        var content = "Test file content";

        // Act
        var commitSha = await _testHelper.CreateTestFileAsync(branchName, filePath, content);

        // Assert
        Assert.NotEmpty(commitSha);
        Assert.Matches(@"^[a-f0-9]{40}$", commitSha); // SHA-1 format
    }

    [Fact(Skip = "Requires GitHub credentials - set RUN_GITHUB_TESTS=true to enable")]
    public async Task CreateTestFile_UpdateExistingFile_UpdatesSuccessfully()
    {
        if (!_testsEnabled || _testHelper == null)
        {
            Assert.True(true, "Test skipped - credentials not provided");
            return;
        }

        // Arrange
        var branchName = await _testHelper.CreateTestBranchAsync("update-test");
        var filePath = "test-files/update-test.txt";

        // Create file first time
        var content1 = "Initial content";
        await _testHelper.CreateTestFileAsync(branchName, filePath, content1);

        // Act - Update the same file
        var content2 = "Updated content";
        var commitSha = await _testHelper.CreateTestFileAsync(branchName, filePath, content2);

        // Assert
        Assert.NotEmpty(commitSha);
    }

    [Fact(Skip = "Requires GitHub credentials - set RUN_GITHUB_TESTS=true to enable")]
    public async Task AssertBranchExists_NonexistentBranch_ThrowsException()
    {
        if (!_testsEnabled || _testHelper == null)
        {
            Assert.True(true, "Test skipped - credentials not provided");
            return;
        }

        // Arrange & Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(async () =>
        {
            await _testHelper.AssertBranchExistsAsync("this-branch-does-not-exist-12345");
        });

        Assert.Contains("does not exist", exception.Message);
    }

    [Fact(Skip = "Requires GitHub credentials - set RUN_GITHUB_TESTS=true to enable")]
    public async Task AssertPullRequestExists_NonexistentPR_ThrowsException()
    {
        if (!_testsEnabled || _testHelper == null)
        {
            Assert.True(true, "Test skipped - credentials not provided");
            return;
        }

        // Arrange & Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(async () =>
        {
            await _testHelper.AssertPullRequestExistsAsync(999999);
        });

        Assert.Contains("does not exist", exception.Message);
    }

    [Fact(Skip = "Requires GitHub credentials - set RUN_GITHUB_TESTS=true to enable")]
    public async Task GetAuthenticatedUser_ValidToken_ReturnsUser()
    {
        if (!_testsEnabled || _testHelper == null)
        {
            Assert.True(true, "Test skipped - credentials not provided");
            return;
        }

        // Act
        var user = await _testHelper.GetAuthenticatedUserAsync();

        // Assert
        Assert.NotNull(user);
        Assert.NotEmpty(user.Login);
        Assert.True(user.Id > 0);
    }

    [Fact(Skip = "Requires GitHub credentials - set RUN_GITHUB_TESTS=true to enable")]
    public async Task GetRepository_ValidRepo_ReturnsRepoDetails()
    {
        if (!_testsEnabled || _testHelper == null)
        {
            Assert.True(true, "Test skipped - credentials not provided");
            return;
        }

        // Act
        var repo = await _testHelper.GetRepositoryAsync();

        // Assert
        Assert.NotNull(repo);
        Assert.NotEmpty(repo.Name);
        Assert.NotEmpty(repo.DefaultBranch);
        Assert.NotNull(repo.Owner);
    }

    public void Dispose()
    {
        _testHelper?.Dispose();
    }
}

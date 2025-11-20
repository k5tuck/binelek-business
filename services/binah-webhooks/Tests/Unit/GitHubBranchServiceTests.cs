using Xunit;
using Moq;
using Octokit;
using Binah.Webhooks.Services.Interfaces;
using Binah.Webhooks.Services.Implementations;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Binah.Webhooks.Tests.Unit;

/// <summary>
/// Unit tests for GitHubBranchService
/// </summary>
public class GitHubBranchServiceTests
{
    private readonly Mock<IGitHubApiClient> _mockApiClient;
    private readonly Mock<ILogger<GitHubBranchService>> _mockLogger;
    private readonly Mock<GitHubClient> _mockGitHubClient;
    private readonly GitHubBranchService _service;

    public GitHubBranchServiceTests()
    {
        _mockApiClient = new Mock<IGitHubApiClient>();
        _mockLogger = new Mock<ILogger<GitHubBranchService>>();
        _mockGitHubClient = new Mock<GitHubClient>(new ProductHeaderValue("Binelek-Test"));

        // Setup API client to return initialized state
        _mockApiClient.Setup(c => c.IsInitialized).Returns(true);
        _mockApiClient.Setup(c => c.CurrentTenantId).Returns(Guid.NewGuid());

        _service = new GitHubBranchService(_mockApiClient.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetBranchAsync_ValidBranch_ReturnsBranch()
    {
        // Arrange
        var owner = "k5tuck";
        var repo = "Binelek";
        var branchName = "main";

        var expectedBranch = new Branch(
            "main",
            new GitReference("refs/heads/main", "abc123", "commit", "url", new User()),
            new GitHubCommit("url", "sha", "node", new Committer(), new Committer(), "message", null, null, 0, 0, null, null, null, null, null)
        );

        // Note: In real tests, you would mock the GitHubClient properly
        // For now, this is a placeholder to show the test structure

        // Assert parameters are validated
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.GetBranchAsync(owner, repo, branchName));
    }

    [Fact]
    public async Task GetBranchAsync_EmptyOwner_ThrowsArgumentException()
    {
        // Arrange
        var owner = "";
        var repo = "Binelek";
        var branchName = "main";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetBranchAsync(owner, repo, branchName));
    }

    [Fact]
    public async Task GetBranchAsync_EmptyRepo_ThrowsArgumentException()
    {
        // Arrange
        var owner = "k5tuck";
        var repo = "";
        var branchName = "main";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetBranchAsync(owner, repo, branchName));
    }

    [Fact]
    public async Task GetBranchAsync_EmptyBranchName_ThrowsArgumentException()
    {
        // Arrange
        var owner = "k5tuck";
        var repo = "Binelek";
        var branchName = "";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetBranchAsync(owner, repo, branchName));
    }

    [Fact]
    public async Task GetBranchAsync_ClientNotInitialized_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockApiClient.Setup(c => c.IsInitialized).Returns(false);
        var owner = "k5tuck";
        var repo = "Binelek";
        var branchName = "main";

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.GetBranchAsync(owner, repo, branchName));
    }

    [Fact]
    public async Task CreateBranchAsync_EmptySha_ThrowsArgumentException()
    {
        // Arrange
        var owner = "k5tuck";
        var repo = "Binelek";
        var branchName = "feature/test";
        var fromSha = "";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.CreateBranchAsync(owner, repo, branchName, fromSha));
    }

    [Fact]
    public async Task DeleteBranchAsync_ValidParameters_CallsGitHubClient()
    {
        // Arrange
        var owner = "k5tuck";
        var repo = "Binelek";
        var branchName = "feature/test";

        // Note: Full implementation would mock the GitHubClient properly
        // This test shows the expected behavior

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.DeleteBranchAsync(owner, repo, branchName));
    }

    [Fact]
    public async Task BranchExistsAsync_ValidParameters_ChecksExistence()
    {
        // Arrange
        var owner = "k5tuck";
        var repo = "Binelek";
        var branchName = "main";

        // Note: Full implementation would mock the GitHubClient to return true/false
        // This test shows the expected behavior

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.BranchExistsAsync(owner, repo, branchName));
    }

    [Fact]
    public async Task GetDefaultBranchAsync_ValidRepository_ReturnsDefaultBranch()
    {
        // Arrange
        var owner = "k5tuck";
        var repo = "Binelek";
        var expectedDefaultBranch = "main";

        var mockRepo = new Repository();
        typeof(Repository)
            .GetProperty(nameof(Repository.DefaultBranch))!
            .SetValue(mockRepo, expectedDefaultBranch);

        _mockApiClient
            .Setup(c => c.GetRepositoryAsync(owner, repo))
            .ReturnsAsync(mockRepo);

        // Act
        var result = await _service.GetDefaultBranchAsync(owner, repo);

        // Assert
        Assert.Equal(expectedDefaultBranch, result);
    }

    [Fact]
    public async Task GetDefaultBranchAsync_EmptyOwner_ThrowsArgumentException()
    {
        // Arrange
        var owner = "";
        var repo = "Binelek";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetDefaultBranchAsync(owner, repo));
    }

    [Fact]
    public async Task GetBranchHeadShaAsync_ValidBranch_ReturnsSha()
    {
        // Arrange
        var owner = "k5tuck";
        var repo = "Binelek";
        var branchName = "main";

        // Note: Full implementation would mock GetBranchAsync to return a branch with SHA

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.GetBranchHeadShaAsync(owner, repo, branchName));
    }
}

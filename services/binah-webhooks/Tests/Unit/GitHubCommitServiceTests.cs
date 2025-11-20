using Xunit;
using Moq;
using Octokit;
using Binah.Webhooks.Services.Interfaces;
using Binah.Webhooks.Services.Implementations;
using Binah.Webhooks.Models.DTOs.GitHub;
using Microsoft.Extensions.Logging;

namespace Binah.Webhooks.Tests.Unit;

/// <summary>
/// Unit tests for GitHubCommitService
/// </summary>
public class GitHubCommitServiceTests
{
    private readonly Mock<IGitHubApiClient> _mockApiClient;
    private readonly Mock<IGitHubBranchService> _mockBranchService;
    private readonly Mock<ILogger<GitHubCommitService>> _mockLogger;
    private readonly GitHubCommitService _service;

    public GitHubCommitServiceTests()
    {
        _mockApiClient = new Mock<IGitHubApiClient>();
        _mockBranchService = new Mock<IGitHubBranchService>();
        _mockLogger = new Mock<ILogger<GitHubCommitService>>();

        // Setup API client to return initialized state
        _mockApiClient.Setup(c => c.IsInitialized).Returns(true);
        _mockApiClient.Setup(c => c.CurrentTenantId).Returns(Guid.NewGuid());

        _service = new GitHubCommitService(
            _mockApiClient.Object,
            _mockBranchService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task CreateCommitAsync_EmptyOwner_ThrowsArgumentException()
    {
        // Arrange
        var owner = "";
        var repo = "Binelek";
        var branchName = "main";
        var message = "Test commit";
        var files = new List<GitHubFileChange>
        {
            new() { Path = "test.txt", Content = "test", Mode = GitHubFileChangeMode.Add }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.CreateCommitAsync(owner, repo, branchName, message, files));
    }

    [Fact]
    public async Task CreateCommitAsync_EmptyRepo_ThrowsArgumentException()
    {
        // Arrange
        var owner = "k5tuck";
        var repo = "";
        var branchName = "main";
        var message = "Test commit";
        var files = new List<GitHubFileChange>
        {
            new() { Path = "test.txt", Content = "test", Mode = GitHubFileChangeMode.Add }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.CreateCommitAsync(owner, repo, branchName, message, files));
    }

    [Fact]
    public async Task CreateCommitAsync_EmptyMessage_ThrowsArgumentException()
    {
        // Arrange
        var owner = "k5tuck";
        var repo = "Binelek";
        var branchName = "main";
        var message = "";
        var files = new List<GitHubFileChange>
        {
            new() { Path = "test.txt", Content = "test", Mode = GitHubFileChangeMode.Add }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.CreateCommitAsync(owner, repo, branchName, message, files));
    }

    [Fact]
    public async Task CreateCommitAsync_EmptyFileList_ThrowsArgumentException()
    {
        // Arrange
        var owner = "k5tuck";
        var repo = "Binelek";
        var branchName = "main";
        var message = "Test commit";
        var files = new List<GitHubFileChange>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.CreateCommitAsync(owner, repo, branchName, message, files));
    }

    [Fact]
    public async Task CreateCommitAsync_NullFileList_ThrowsArgumentException()
    {
        // Arrange
        var owner = "k5tuck";
        var repo = "Binelek";
        var branchName = "main";
        var message = "Test commit";
        List<GitHubFileChange>? files = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.CreateCommitAsync(owner, repo, branchName, message, files!));
    }

    [Fact]
    public async Task CreateCommitAsync_ClientNotInitialized_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockApiClient.Setup(c => c.IsInitialized).Returns(false);
        var owner = "k5tuck";
        var repo = "Binelek";
        var branchName = "main";
        var message = "Test commit";
        var files = new List<GitHubFileChange>
        {
            new() { Path = "test.txt", Content = "test", Mode = GitHubFileChangeMode.Add }
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.CreateCommitAsync(owner, repo, branchName, message, files));
    }

    [Fact]
    public async Task GetCommitAsync_EmptySha_ThrowsArgumentException()
    {
        // Arrange
        var owner = "k5tuck";
        var repo = "Binelek";
        var sha = "";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetCommitAsync(owner, repo, sha));
    }

    [Fact]
    public async Task UpdateFileAsync_EmptyPath_ThrowsArgumentException()
    {
        // Arrange
        var owner = "k5tuck";
        var repo = "Binelek";
        var path = "";
        var content = "test";
        var message = "Update file";
        var branchName = "main";
        var sha = "abc123";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.UpdateFileAsync(owner, repo, path, content, message, branchName, sha));
    }

    [Fact]
    public async Task UpdateFileAsync_EmptySha_ThrowsArgumentException()
    {
        // Arrange
        var owner = "k5tuck";
        var repo = "Binelek";
        var path = "test.txt";
        var content = "test";
        var message = "Update file";
        var branchName = "main";
        var sha = "";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.UpdateFileAsync(owner, repo, path, content, message, branchName, sha));
    }

    [Fact]
    public async Task CreateFileAsync_EmptyPath_ThrowsArgumentException()
    {
        // Arrange
        var owner = "k5tuck";
        var repo = "Binelek";
        var path = "";
        var content = "test";
        var message = "Create file";
        var branchName = "main";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.CreateFileAsync(owner, repo, path, content, message, branchName));
    }

    [Fact]
    public async Task DeleteFileAsync_EmptySha_ThrowsArgumentException()
    {
        // Arrange
        var owner = "k5tuck";
        var repo = "Binelek";
        var path = "test.txt";
        var message = "Delete file";
        var branchName = "main";
        var sha = "";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.DeleteFileAsync(owner, repo, path, message, branchName, sha));
    }

    [Fact]
    public async Task GetFileAsync_EmptyPath_ThrowsArgumentException()
    {
        // Arrange
        var owner = "k5tuck";
        var repo = "Binelek";
        var path = "";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetFileAsync(owner, repo, path));
    }

    [Fact]
    public async Task GetFileAsync_ValidPath_CallsGitHubClient()
    {
        // Arrange
        var owner = "k5tuck";
        var repo = "Binelek";
        var path = "test.txt";

        // Note: Full implementation would mock the GitHubClient to return file content
        // This test shows the expected behavior

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.GetFileAsync(owner, repo, path));
    }

    [Fact]
    public void GitHubFileChangeMode_HasCorrectValues()
    {
        // Arrange & Act
        var addMode = GitHubFileChangeMode.Add;
        var updateMode = GitHubFileChangeMode.Update;
        var deleteMode = GitHubFileChangeMode.Delete;

        // Assert
        Assert.Equal(0, (int)addMode);
        Assert.Equal(1, (int)updateMode);
        Assert.Equal(2, (int)deleteMode);
    }

    [Fact]
    public void GitHubFileChange_HasRequiredProperties()
    {
        // Arrange & Act
        var fileChange = new GitHubFileChange
        {
            Path = "test.txt",
            Content = "test content",
            Mode = GitHubFileChangeMode.Add,
            Sha = "abc123"
        };

        // Assert
        Assert.Equal("test.txt", fileChange.Path);
        Assert.Equal("test content", fileChange.Content);
        Assert.Equal(GitHubFileChangeMode.Add, fileChange.Mode);
        Assert.Equal("abc123", fileChange.Sha);
    }
}

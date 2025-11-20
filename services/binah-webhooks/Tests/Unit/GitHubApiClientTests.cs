using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Binah.Webhooks.Services.Implementations;
using Binah.Webhooks.Services.Interfaces;
using Binah.Webhooks.Repositories.Interfaces;
using Binah.Webhooks.Models.Domain;

namespace Binah.Webhooks.Tests.Unit;

/// <summary>
/// Unit tests for GitHubApiClient
/// </summary>
public class GitHubApiClientTests
{
    private readonly Mock<IGitHubOAuthTokenRepository> _mockTokenRepository;
    private readonly Mock<ILogger<GitHubApiClient>> _mockLogger;
    private readonly GitHubApiClient _apiClient;

    public GitHubApiClientTests()
    {
        _mockTokenRepository = new Mock<IGitHubOAuthTokenRepository>();
        _mockLogger = new Mock<ILogger<GitHubApiClient>>();
        _apiClient = new GitHubApiClient(_mockTokenRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task InitializeForTenantAsync_ValidToken_ReturnsTrue()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var token = new GitHubOAuthToken
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AccessToken = "gho_test_token_123456789",
            TokenType = "Bearer",
            CreatedAt = DateTime.UtcNow
        };

        _mockTokenRepository
            .Setup(r => r.GetByTenantAsync(tenantId))
            .ReturnsAsync(token);

        // Act
        var result = await _apiClient.InitializeForTenantAsync(tenantId);

        // Assert
        Assert.True(result);
        Assert.True(_apiClient.IsInitialized);
        Assert.Equal(tenantId, _apiClient.CurrentTenantId);
    }

    [Fact]
    public async Task InitializeForTenantAsync_NoToken_ReturnsFalse()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        _mockTokenRepository
            .Setup(r => r.GetByTenantAsync(tenantId))
            .ReturnsAsync((GitHubOAuthToken?)null);

        // Act
        var result = await _apiClient.InitializeForTenantAsync(tenantId);

        // Assert
        Assert.False(result);
        Assert.False(_apiClient.IsInitialized);
        Assert.Null(_apiClient.CurrentTenantId);
    }

    [Fact]
    public async Task GetAuthenticatedUserAsync_NotInitialized_ThrowsInvalidOperationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _apiClient.GetAuthenticatedUserAsync());
    }

    [Fact]
    public async Task GetRepositoryAsync_NotInitialized_ThrowsInvalidOperationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _apiClient.GetRepositoryAsync("owner", "repo"));
    }

    [Fact]
    public async Task GetRepositoryAsync_EmptyOwner_ThrowsArgumentException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var token = new GitHubOAuthToken
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AccessToken = "gho_test_token_123456789",
            TokenType = "Bearer",
            CreatedAt = DateTime.UtcNow
        };

        _mockTokenRepository
            .Setup(r => r.GetByTenantAsync(tenantId))
            .ReturnsAsync(token);

        await _apiClient.InitializeForTenantAsync(tenantId);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _apiClient.GetRepositoryAsync("", "repo"));
    }

    [Fact]
    public async Task GetRepositoryAsync_EmptyName_ThrowsArgumentException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var token = new GitHubOAuthToken
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AccessToken = "gho_test_token_123456789",
            TokenType = "Bearer",
            CreatedAt = DateTime.UtcNow
        };

        _mockTokenRepository
            .Setup(r => r.GetByTenantAsync(tenantId))
            .ReturnsAsync(token);

        await _apiClient.InitializeForTenantAsync(tenantId);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _apiClient.GetRepositoryAsync("owner", ""));
    }

    [Fact]
    public async Task InitializeForTenantAsync_RepositoryThrowsException_ThrowsException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        _mockTokenRepository
            .Setup(r => r.GetByTenantAsync(tenantId))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(
            () => _apiClient.InitializeForTenantAsync(tenantId));
    }

    [Fact]
    public void IsInitialized_BeforeInitialization_ReturnsFalse()
    {
        // Assert
        Assert.False(_apiClient.IsInitialized);
    }

    [Fact]
    public void CurrentTenantId_BeforeInitialization_ReturnsNull()
    {
        // Assert
        Assert.Null(_apiClient.CurrentTenantId);
    }

    [Fact]
    public async Task InitializeForTenantAsync_MultipleCalls_UpdatesTenantId()
    {
        // Arrange
        var tenantId1 = Guid.NewGuid();
        var tenantId2 = Guid.NewGuid();

        var token1 = new GitHubOAuthToken
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId1,
            AccessToken = "gho_test_token_tenant1",
            TokenType = "Bearer",
            CreatedAt = DateTime.UtcNow
        };

        var token2 = new GitHubOAuthToken
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId2,
            AccessToken = "gho_test_token_tenant2",
            TokenType = "Bearer",
            CreatedAt = DateTime.UtcNow
        };

        _mockTokenRepository
            .Setup(r => r.GetByTenantAsync(tenantId1))
            .ReturnsAsync(token1);

        _mockTokenRepository
            .Setup(r => r.GetByTenantAsync(tenantId2))
            .ReturnsAsync(token2);

        // Act
        await _apiClient.InitializeForTenantAsync(tenantId1);
        Assert.Equal(tenantId1, _apiClient.CurrentTenantId);

        await _apiClient.InitializeForTenantAsync(tenantId2);

        // Assert
        Assert.Equal(tenantId2, _apiClient.CurrentTenantId);
        Assert.True(_apiClient.IsInitialized);
    }
}

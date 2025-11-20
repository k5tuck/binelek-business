using System;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Binah.Webhooks.Models.Domain;
using Binah.Webhooks.Models.DTOs.GitHub;
using Binah.Webhooks.Services.Implementations;
using Binah.Webhooks.Services.Interfaces;
using Binah.Webhooks.Repositories.Interfaces;
using Binah.Infrastructure.Kafka;

namespace Binah.Webhooks.Tests.Integration;

/// <summary>
/// Integration tests for GitHub webhook end-to-end flow
/// Tests: Receive → Verify → Store → Publish to Kafka → Mark as Processed
/// </summary>
public class GitHubWebhookIntegrationTests
{
    private readonly Mock<ILogger<GitHubWebhookService>> _mockLogger;
    private readonly Mock<ILogger<GitHubEventPublisher>> _mockPublisherLogger;
    private readonly Mock<IGitHubWebhookEventRepository> _mockRepository;
    private readonly Mock<KafkaProducer> _mockKafkaProducer;
    private readonly IGitHubEventPublisher _eventPublisher;
    private readonly IGitHubWebhookService _webhookService;

    public GitHubWebhookIntegrationTests()
    {
        _mockLogger = new Mock<ILogger<GitHubWebhookService>>();
        _mockPublisherLogger = new Mock<ILogger<GitHubEventPublisher>>();
        _mockRepository = new Mock<IGitHubWebhookEventRepository>();
        _mockKafkaProducer = new Mock<KafkaProducer>("localhost:9092");

        _eventPublisher = new GitHubEventPublisher(
            _mockKafkaProducer.Object,
            _mockPublisherLogger.Object);

        _webhookService = new GitHubWebhookService(
            _mockLogger.Object,
            _mockRepository.Object,
            _eventPublisher);
    }

    [Fact]
    public async Task E2E_ProcessWebhook_ValidPushEvent_StoresAndPublishes()
    {
        // Arrange
        var tenantId = Guid.NewGuid().ToString();
        var deliveryId = Guid.NewGuid().ToString();
        var eventType = "push";
        var payload = CreateSamplePushPayload();

        var storedEvent = new GitHubWebhookEvent
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.Parse(tenantId),
            EventType = eventType,
            RepositoryName = "k5tuck/Binelek",
            Payload = payload,
            Signature = "sha256=abc123",
            ReceivedAt = DateTime.UtcNow,
            Processed = false
        };

        _mockRepository
            .Setup(r => r.CreateAsync(It.IsAny<GitHubWebhookEvent>()))
            .ReturnsAsync(storedEvent);

        _mockRepository
            .Setup(r => r.MarkAsProcessedAsync(It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        _mockKafkaProducer
            .Setup(k => k.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _webhookService.ProcessWebhookAsync(eventType, deliveryId, payload, tenantId);

        // Assert
        Assert.True(result);

        // Verify event was stored in database
        _mockRepository.Verify(
            r => r.CreateAsync(It.Is<GitHubWebhookEvent>(e =>
                e.EventType == eventType &&
                e.RepositoryName == "k5tuck/Binelek" &&
                e.Payload == payload)),
            Times.Once);

        // Verify event was published to Kafka
        _mockKafkaProducer.Verify(
            k => k.ProduceAsync(
                "github.event.received.v1",
                It.IsAny<object>(),
                It.IsAny<string>()),
            Times.Once);

        // Verify event was marked as processed
        _mockRepository.Verify(
            r => r.MarkAsProcessedAsync(storedEvent.Id),
            Times.Once);
    }

    [Fact]
    public async Task E2E_ProcessWebhook_ValidPullRequestEvent_StoresAndPublishes()
    {
        // Arrange
        var tenantId = Guid.NewGuid().ToString();
        var deliveryId = Guid.NewGuid().ToString();
        var eventType = "pull_request";
        var payload = CreateSamplePullRequestPayload();

        var storedEvent = new GitHubWebhookEvent
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.Parse(tenantId),
            EventType = eventType,
            RepositoryName = "k5tuck/Binelek",
            Payload = payload,
            Signature = "sha256=def456",
            ReceivedAt = DateTime.UtcNow,
            Processed = false
        };

        _mockRepository
            .Setup(r => r.CreateAsync(It.IsAny<GitHubWebhookEvent>()))
            .ReturnsAsync(storedEvent);

        _mockRepository
            .Setup(r => r.MarkAsProcessedAsync(It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        _mockKafkaProducer
            .Setup(k => k.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _webhookService.ProcessWebhookAsync(eventType, deliveryId, payload, tenantId);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<GitHubWebhookEvent>()), Times.Once);
        _mockKafkaProducer.Verify(k => k.ProduceAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>()), Times.Once);
        _mockRepository.Verify(r => r.MarkAsProcessedAsync(storedEvent.Id), Times.Once);
    }

    [Fact]
    public async Task E2E_ProcessWebhook_InvalidTenantId_ReturnsFalse()
    {
        // Arrange
        var tenantId = "invalid-guid";
        var deliveryId = Guid.NewGuid().ToString();
        var eventType = "push";
        var payload = CreateSamplePushPayload();

        // Act
        var result = await _webhookService.ProcessWebhookAsync(eventType, deliveryId, payload, tenantId);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<GitHubWebhookEvent>()), Times.Never);
        _mockKafkaProducer.Verify(k => k.ProduceAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task E2E_ProcessWebhook_RepositoryThrowsException_ReturnsFalse()
    {
        // Arrange
        var tenantId = Guid.NewGuid().ToString();
        var deliveryId = Guid.NewGuid().ToString();
        var eventType = "push";
        var payload = CreateSamplePushPayload();

        _mockRepository
            .Setup(r => r.CreateAsync(It.IsAny<GitHubWebhookEvent>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var result = await _webhookService.ProcessWebhookAsync(eventType, deliveryId, payload, tenantId);

        // Assert
        Assert.False(result);
        _mockKafkaProducer.Verify(k => k.ProduceAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void VerifyGitHubSignature_ValidSignature_ReturnsTrue()
    {
        // Arrange
        var payload = "{\"test\":\"data\"}";
        var secret = "my-secret-key";
        var signature = ComputeGitHubSignature(payload, secret);

        // Act
        var result = _webhookService.VerifyGitHubSignature(payload, signature, secret);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyGitHubSignature_InvalidSignature_ReturnsFalse()
    {
        // Arrange
        var payload = "{\"test\":\"data\"}";
        var secret = "my-secret-key";
        var invalidSignature = "sha256=invalid";

        // Act
        var result = _webhookService.VerifyGitHubSignature(payload, invalidSignature, secret);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyGitHubSignature_EmptyPayload_ReturnsFalse()
    {
        // Arrange
        var payload = string.Empty;
        var secret = "my-secret-key";
        var signature = "sha256=test";

        // Act
        var result = _webhookService.VerifyGitHubSignature(payload, signature, secret);

        // Assert
        Assert.False(result);
    }

    #region Helper Methods

    private string CreateSamplePushPayload()
    {
        var payload = new
        {
            @ref = "refs/heads/main",
            repository = new
            {
                id = 123456,
                name = "Binelek",
                full_name = "k5tuck/Binelek",
                @private = false,
                owner = new
                {
                    login = "k5tuck",
                    id = 789,
                    type = "User"
                },
                html_url = "https://github.com/k5tuck/Binelek",
                description = "Universal Multi-Domain Knowledge Graph Platform",
                default_branch = "main"
            },
            sender = new
            {
                login = "k5tuck",
                id = 789,
                type = "User"
            },
            commits = new[]
            {
                new
                {
                    id = "abc123",
                    message = "test: Add GitHub webhook integration",
                    author = new { name = "Developer", email = "dev@example.com" }
                }
            }
        };

        return JsonSerializer.Serialize(payload);
    }

    private string CreateSamplePullRequestPayload()
    {
        var payload = new
        {
            action = "opened",
            pull_request = new
            {
                number = 42,
                title = "Add GitHub integration",
                body = "This PR adds GitHub webhook support",
                state = "open",
                html_url = "https://github.com/k5tuck/Binelek/pull/42"
            },
            repository = new
            {
                id = 123456,
                name = "Binelek",
                full_name = "k5tuck/Binelek",
                @private = false,
                owner = new
                {
                    login = "k5tuck",
                    id = 789,
                    type = "User"
                }
            },
            sender = new
            {
                login = "k5tuck",
                id = 789,
                type = "User"
            }
        };

        return JsonSerializer.Serialize(payload);
    }

    private string ComputeGitHubSignature(string payload, string secret)
    {
        using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload));
        return "sha256=" + BitConverter.ToString(hash).Replace("-", "").ToLower();
    }

    #endregion
}

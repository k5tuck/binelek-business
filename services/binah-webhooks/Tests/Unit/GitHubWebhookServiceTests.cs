using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Binah.Webhooks.Services.Interfaces;
using Binah.Webhooks.Services.Implementations;
using System.Security.Cryptography;
using System.Text;

namespace Binah.Webhooks.Tests.Unit;

/// <summary>
/// Unit tests for GitHubWebhookService signature verification
/// </summary>
public class GitHubWebhookServiceTests
{
    private readonly IGitHubWebhookService _service;
    private readonly Mock<ILogger<GitHubWebhookService>> _loggerMock;

    public GitHubWebhookServiceTests()
    {
        _loggerMock = new Mock<ILogger<GitHubWebhookService>>();
        _service = new GitHubWebhookService(_loggerMock.Object);
    }

    [Fact]
    public void VerifyGitHubSignature_ValidSignature_ReturnsTrue()
    {
        // Arrange
        var payload = "{\"action\":\"opened\",\"number\":1}";
        var secret = "test-webhook-secret-123";
        var signature = ComputeGitHubSignature(payload, secret);

        // Act
        var result = _service.VerifyGitHubSignature(payload, signature, secret);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyGitHubSignature_InvalidSignature_ReturnsFalse()
    {
        // Arrange
        var payload = "{\"action\":\"opened\",\"number\":1}";
        var secret = "test-webhook-secret-123";
        var invalidSignature = "sha256=invalid_signature_hash";

        // Act
        var result = _service.VerifyGitHubSignature(payload, invalidSignature, secret);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyGitHubSignature_TamperedPayload_ReturnsFalse()
    {
        // Arrange
        var originalPayload = "{\"action\":\"opened\",\"number\":1}";
        var secret = "test-webhook-secret-123";
        var signature = ComputeGitHubSignature(originalPayload, secret);

        var tamperedPayload = "{\"action\":\"opened\",\"number\":2}"; // Changed number

        // Act
        var result = _service.VerifyGitHubSignature(tamperedPayload, signature, secret);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyGitHubSignature_WrongSecret_ReturnsFalse()
    {
        // Arrange
        var payload = "{\"action\":\"opened\",\"number\":1}";
        var correctSecret = "correct-secret";
        var wrongSecret = "wrong-secret";
        var signature = ComputeGitHubSignature(payload, correctSecret);

        // Act
        var result = _service.VerifyGitHubSignature(payload, signature, wrongSecret);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyGitHubSignature_EmptyPayload_ReturnsFalse()
    {
        // Arrange
        var payload = string.Empty;
        var secret = "test-secret";
        var signature = "sha256=some_hash";

        // Act
        var result = _service.VerifyGitHubSignature(payload, signature, secret);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyGitHubSignature_EmptySignature_ReturnsFalse()
    {
        // Arrange
        var payload = "{\"action\":\"opened\"}";
        var secret = "test-secret";
        var signature = string.Empty;

        // Act
        var result = _service.VerifyGitHubSignature(payload, signature, secret);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyGitHubSignature_EmptySecret_ReturnsFalse()
    {
        // Arrange
        var payload = "{\"action\":\"opened\"}";
        var secret = string.Empty;
        var signature = "sha256=some_hash";

        // Act
        var result = _service.VerifyGitHubSignature(payload, signature, secret);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyGitHubSignature_ComplexPayload_ReturnsTrue()
    {
        // Arrange
        var payload = @"{
            ""action"": ""opened"",
            ""pull_request"": {
                ""id"": 1,
                ""title"": ""Add new feature"",
                ""body"": ""This PR adds a new feature with special chars: !@#$%^&*()"",
                ""user"": {
                    ""login"": ""testuser""
                }
            },
            ""repository"": {
                ""name"": ""test-repo"",
                ""full_name"": ""org/test-repo""
            }
        }";
        var secret = "complex-secret-with-special-chars-!@#$%";
        var signature = ComputeGitHubSignature(payload, secret);

        // Act
        var result = _service.VerifyGitHubSignature(payload, signature, secret);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyGitHubSignature_SignatureWithoutPrefix_ReturnsFalse()
    {
        // Arrange
        var payload = "{\"action\":\"opened\"}";
        var secret = "test-secret";
        var signatureWithoutPrefix = ComputeGitHubSignature(payload, secret).Replace("sha256=", "");

        // Act
        var result = _service.VerifyGitHubSignature(payload, signatureWithoutPrefix, secret);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ProcessWebhookAsync_ValidParameters_ReturnsTrue()
    {
        // Arrange
        var eventType = "push";
        var deliveryId = Guid.NewGuid().ToString();
        var payload = "{\"ref\":\"refs/heads/main\"}";
        var tenantId = "test-tenant";

        // Act
        var result = await _service.ProcessWebhookAsync(eventType, deliveryId, payload, tenantId);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Helper method to compute GitHub-style HMAC-SHA256 signature
    /// This mimics what GitHub does when sending webhooks
    /// </summary>
    private string ComputeGitHubSignature(string payload, string secret)
    {
        var encoding = new UTF8Encoding();
        var keyBytes = encoding.GetBytes(secret);
        var payloadBytes = encoding.GetBytes(payload);

        using (var hmac = new HMACSHA256(keyBytes))
        {
            var hash = hmac.ComputeHash(payloadBytes);
            return "sha256=" + BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}

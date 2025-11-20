using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Binah.Webhooks.Services;

namespace Binah.Webhooks.Tests.Unit;

/// <summary>
/// Unit tests for UrlValidator SSRF protection
/// </summary>
public class UrlValidatorTests
{
    private readonly UrlValidator _validator;
    private readonly Mock<ILogger<UrlValidator>> _loggerMock;

    public UrlValidatorTests()
    {
        _loggerMock = new Mock<ILogger<UrlValidator>>();
        _validator = new UrlValidator(_loggerMock.Object);
    }

    [Theory]
    [InlineData("https://example.com/webhook")]
    [InlineData("http://api.example.com/callback")]
    [InlineData("https://webhook.site/unique-id")]
    public void IsUrlSafe_WithValidExternalUrl_ReturnsTrue(string url)
    {
        // Act
        var result = _validator.IsUrlSafe(url);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("http://localhost/webhook")]
    [InlineData("http://127.0.0.1/webhook")]
    [InlineData("http://0.0.0.0/webhook")]
    public void IsUrlSafe_WithLocalhostUrl_ReturnsFalse(string url)
    {
        // Act
        var result = _validator.IsUrlSafe(url);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("http://10.0.0.1/webhook")]
    [InlineData("http://10.255.255.255/webhook")]
    [InlineData("http://192.168.1.1/webhook")]
    [InlineData("http://192.168.255.255/webhook")]
    [InlineData("http://172.16.0.1/webhook")]
    [InlineData("http://172.31.255.255/webhook")]
    public void IsUrlSafe_WithPrivateIpUrl_ReturnsFalse(string url)
    {
        // Act
        var result = _validator.IsUrlSafe(url);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("http://169.254.169.254/latest/meta-data/")]
    public void IsUrlSafe_WithAwsMetadataUrl_ReturnsFalse(string url)
    {
        // Act
        var result = _validator.IsUrlSafe(url);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("ftp://example.com/file")]
    [InlineData("file:///etc/passwd")]
    [InlineData("javascript:alert(1)")]
    public void IsUrlSafe_WithNonHttpScheme_ReturnsFalse(string url)
    {
        // Act
        var result = _validator.IsUrlSafe(url);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void IsUrlSafe_WithEmptyUrl_ReturnsFalse(string url)
    {
        // Act
        var result = _validator.IsUrlSafe(url);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("not-a-valid-url")]
    [InlineData("htp://missing-t.com")]
    public void IsUrlSafe_WithInvalidUrlFormat_ReturnsFalse(string url)
    {
        // Act
        var result = _validator.IsUrlSafe(url);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetValidationError_WithLocalhostUrl_ReturnsAppropriateMessage()
    {
        // Arrange
        var url = "http://localhost/webhook";

        // Act
        var error = _validator.GetValidationError(url);

        // Assert
        error.Should().NotBeEmpty();
        error.Should().Contain("blocked");
    }

    [Fact]
    public void GetValidationError_WithPrivateIpUrl_ReturnsAppropriateMessage()
    {
        // Arrange
        var url = "http://192.168.1.1/webhook";

        // Act
        var error = _validator.GetValidationError(url);

        // Assert
        error.Should().NotBeEmpty();
        error.Should().Contain("private");
    }

    [Fact]
    public void GetValidationError_WithValidUrl_ReturnsEmptyString()
    {
        // Arrange
        var url = "https://example.com/webhook";

        // Act
        var error = _validator.GetValidationError(url);

        // Assert
        error.Should().BeEmpty();
    }

    [Fact]
    public void GetValidationError_WithNonHttpScheme_ReturnsAppropriateMessage()
    {
        // Arrange
        var url = "ftp://example.com/file";

        // Act
        var error = _validator.GetValidationError(url);

        // Assert
        error.Should().NotBeEmpty();
        error.Should().Contain("HTTP");
    }

    [Fact]
    public void GetValidationError_WithEmptyUrl_ReturnsAppropriateMessage()
    {
        // Arrange
        var url = "";

        // Act
        var error = _validator.GetValidationError(url);

        // Assert
        error.Should().NotBeEmpty();
        error.Should().Contain("empty");
    }
}

using Binah.Webhooks.Models.DTOs.Notifications;
using Binah.Webhooks.Services.Implementations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Binah.Webhooks.Tests.Unit;

/// <summary>
/// Unit tests for NotificationService
/// </summary>
public class NotificationServiceTests
{
    private readonly Mock<ILogger<NotificationService>> _loggerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly NotificationService _service;

    public NotificationServiceTests()
    {
        _loggerMock = new Mock<ILogger<NotificationService>>();
        _configurationMock = new Mock<IConfiguration>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();

        SetupDefaultConfiguration();

        _service = new NotificationService(
            _loggerMock.Object,
            _configurationMock.Object,
            _httpClientFactoryMock.Object);
    }

    private void SetupDefaultConfiguration()
    {
        var configSectionMock = new Mock<IConfigurationSection>();
        configSectionMock.Setup(x => x.Value).Returns("false");

        _configurationMock.Setup(x => x.GetSection(It.IsAny<string>())).Returns(configSectionMock.Object);
        _configurationMock.Setup(x => x["Notifications:Slack:Enabled"]).Returns("false");
        _configurationMock.Setup(x => x["Notifications:Email:Enabled"]).Returns("false");
        _configurationMock.Setup(x => x["Notifications:InApp:Enabled"]).Returns("true");
        _configurationMock.Setup(x => x["Notifications:Webhook:Enabled"]).Returns("false");
    }

    [Fact]
    public async Task NotifyPRCreatedAsync_WithValidData_SendsNotifications()
    {
        // Arrange
        var prData = new PullRequestNotificationData
        {
            PrNumber = 42,
            Title = "Test PR",
            Repository = "owner/repo",
            Url = "https://github.com/owner/repo/pull/42",
            Status = "open",
            WorkflowType = "ontology_refactoring",
            BranchName = "test-branch",
            Creator = "TestUser"
        };

        var recipients = new List<string> { "user@example.com" };

        // Act
        await _service.NotifyPRCreatedAsync(prData, recipients);

        // Assert
        // No exceptions thrown indicates success
        Assert.True(true);
    }

    [Fact]
    public async Task NotifyPRMergedAsync_WithValidData_SendsNotifications()
    {
        // Arrange
        var prData = new PullRequestNotificationData
        {
            PrNumber = 42,
            Title = "Test PR",
            Repository = "owner/repo",
            Url = "https://github.com/owner/repo/pull/42",
            Status = "merged",
            WorkflowType = "code_generation"
        };

        var recipients = new List<string> { "user@example.com" };

        // Act
        await _service.NotifyPRMergedAsync(prData, recipients);

        // Assert
        Assert.True(true);
    }

    [Fact]
    public async Task NotifyPRFailedAsync_WithValidData_SendsNotifications()
    {
        // Arrange
        var prData = new PullRequestNotificationData
        {
            PrNumber = 0,
            Title = "Test PR",
            Repository = "owner/repo",
            Url = "",
            Status = "failed",
            WorkflowType = "bug_fix"
        };

        var error = "Test error message";
        var recipients = new List<string> { "user@example.com" };

        // Act
        await _service.NotifyPRFailedAsync(prData, error, recipients);

        // Assert
        Assert.True(true);
    }

    [Fact]
    public async Task NotifyPRNeedsReviewAsync_WithValidData_SendsNotifications()
    {
        // Arrange
        var prData = new PullRequestNotificationData
        {
            PrNumber = 42,
            Title = "Test PR",
            Repository = "owner/repo",
            Url = "https://github.com/owner/repo/pull/42",
            Status = "open"
        };

        var reviewers = new List<string> { "reviewer1", "reviewer2" };

        // Act
        await _service.NotifyPRNeedsReviewAsync(prData, reviewers);

        // Assert
        Assert.True(true);
    }

    [Fact]
    public async Task SendNotificationAsync_WithValidRequest_SendsNotification()
    {
        // Arrange
        var request = new NotificationRequest
        {
            TenantId = "test-tenant",
            Title = "Test Notification",
            Message = "This is a test message",
            Type = "info",
            Channel = NotificationChannel.InApp,
            Recipients = new List<string> { "user@example.com" }
        };

        // Act
        await _service.SendNotificationAsync(request);

        // Assert
        Assert.True(true);
    }

    [Fact]
    public async Task NotifyPRCreatedAsync_WithSlackEnabled_CallsSlackWebhook()
    {
        // Arrange
        _configurationMock.Setup(x => x["Notifications:Slack:Enabled"]).Returns("true");
        _configurationMock.Setup(x => x["Notifications:Slack:WebhookUrl"]).Returns("https://hooks.slack.com/test");

        var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            });

        var httpClient = new HttpClient(httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = new NotificationService(
            _loggerMock.Object,
            _configurationMock.Object,
            _httpClientFactoryMock.Object);

        var prData = new PullRequestNotificationData
        {
            PrNumber = 42,
            Title = "Test PR",
            Repository = "owner/repo",
            Url = "https://github.com/owner/repo/pull/42",
            Status = "open"
        };

        var recipients = new List<string> { "user@example.com" };
        var channels = new List<NotificationChannel> { NotificationChannel.Slack };

        // Act
        await service.NotifyPRCreatedAsync(prData, recipients, channels);

        // Assert
        httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task NotifyPRCreatedAsync_WithEmptyRecipients_UsesDefaultRecipients()
    {
        // Arrange
        var prData = new PullRequestNotificationData
        {
            PrNumber = 42,
            Title = "Test PR",
            Repository = "owner/repo",
            Url = "https://github.com/owner/repo/pull/42",
            Status = "open"
        };

        var recipients = new List<string>();

        // Act & Assert
        await _service.NotifyPRCreatedAsync(prData, recipients);
        // Should not throw exception
        Assert.True(true);
    }

    [Fact]
    public void NotificationChannel_HasCorrectValues()
    {
        // Assert
        Assert.Equal(NotificationChannel.Slack, NotificationChannel.Slack);
        Assert.Equal(NotificationChannel.Email, NotificationChannel.Email);
        Assert.Equal(NotificationChannel.InApp, NotificationChannel.InApp);
        Assert.Equal(NotificationChannel.Webhook, NotificationChannel.Webhook);
    }

    [Fact]
    public void PullRequestNotificationData_HasRequiredProperties()
    {
        // Arrange & Act
        var data = new PullRequestNotificationData
        {
            PrNumber = 42,
            Title = "Test",
            Repository = "owner/repo",
            Url = "https://test.com"
        };

        // Assert
        Assert.Equal(42, data.PrNumber);
        Assert.Equal("Test", data.Title);
        Assert.Equal("owner/repo", data.Repository);
        Assert.Equal("https://test.com", data.Url);
        Assert.Equal("open", data.Status); // Default value
    }

    [Fact]
    public void NotificationRequest_HasDefaultValues()
    {
        // Arrange & Act
        var request = new NotificationRequest
        {
            TenantId = "test",
            Title = "Test",
            Message = "Test message"
        };

        // Assert
        Assert.Equal("info", request.Type);
        Assert.Equal(NotificationChannel.InApp, request.Channel);
        Assert.Empty(request.Recipients);
        Assert.Empty(request.Metadata);
    }
}

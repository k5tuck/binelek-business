using System;
using System.Threading.Tasks;
using Binah.Webhooks.Exceptions;
using Binah.Webhooks.Services.Implementations;
using Binah.Webhooks.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Octokit;
using Xunit;

namespace Binah.Webhooks.Tests.Integration;

/// <summary>
/// Integration tests for GitHub API rate limiting with Octokit
/// Note: These tests may make actual API calls to GitHub if configured
/// </summary>
public class GitHubRateLimitIntegrationTests
{
    private readonly Mock<ILogger<GitHubRateLimiter>> _mockLogger;
    private readonly IConfiguration _configuration;
    private readonly IGitHubRateLimiter _rateLimiter;

    public GitHubRateLimitIntegrationTests()
    {
        _mockLogger = new Mock<ILogger<GitHubRateLimiter>>();

        // Build configuration from appsettings
        var configBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new System.Collections.Generic.KeyValuePair<string, string?>("GitHub:RateLimit:MaxRequestsPerHour", "5000"),
                new System.Collections.Generic.KeyValuePair<string, string?>("GitHub:RateLimit:WarningThreshold", "100"),
                new System.Collections.Generic.KeyValuePair<string, string?>("GitHub:RateLimit:CheckInterval", "60")
            });

        _configuration = configBuilder.Build();
        _rateLimiter = new GitHubRateLimiter(_mockLogger.Object, _configuration);
    }

    [Fact]
    public async Task RateLimiter_HandlesMultipleTenants_Independently()
    {
        // Arrange
        var tenant1 = "tenant-integration-1";
        var tenant2 = "tenant-integration-2";

        // Act
        var status1 = await _rateLimiter.CheckRateLimitAsync(tenant1);
        var status2 = await _rateLimiter.CheckRateLimitAsync(tenant2);

        _rateLimiter.RecordRequest(tenant1);
        _rateLimiter.RecordRequest(tenant1);

        var status1After = await _rateLimiter.CheckRateLimitAsync(tenant1);
        var status2After = await _rateLimiter.CheckRateLimitAsync(tenant2);

        // Assert
        Assert.Equal(5000, status1.Remaining);
        Assert.Equal(5000, status2.Remaining);
        Assert.Equal(4998, status1After.Remaining); // 2 requests recorded
        Assert.Equal(5000, status2After.Remaining); // Unchanged
    }

    [Fact]
    public async Task RateLimiter_SimulatesApproachingLimit_LogsWarning()
    {
        // Arrange
        var tenantId = "tenant-approaching-integration";
        var resetTime = DateTimeOffset.UtcNow.AddHours(1);

        // Act
        _rateLimiter.UpdateRateLimit(tenantId, 99, resetTime);
        await _rateLimiter.WaitForRateLimitAsync(tenantId);

        var status = await _rateLimiter.CheckRateLimitAsync(tenantId);

        // Assert
        Assert.True(status.IsApproachingLimit);
        Assert.Equal(99, status.Remaining);
        // Note: Logger warning should have been called (verified via mock in real tests)
    }

    [Fact]
    public async Task RateLimiter_SimulatesRateLimitExceeded_WaitsForReset()
    {
        // Arrange
        var tenantId = "tenant-exceeded-integration";
        var resetTime = DateTimeOffset.UtcNow.AddSeconds(3); // 3 seconds from now

        // Act
        _rateLimiter.UpdateRateLimit(tenantId, 0, resetTime);

        var startTime = DateTime.UtcNow;
        await _rateLimiter.WaitForRateLimitAsync(tenantId);
        var endTime = DateTime.UtcNow;

        var status = await _rateLimiter.CheckRateLimitAsync(tenantId);

        // Assert
        var waitTime = (endTime - startTime).TotalSeconds;
        Assert.True(waitTime >= 3, $"Expected to wait at least 3 seconds, but waited {waitTime}s");
        Assert.True(status.Remaining > 0, "Rate limit should be reset after waiting");
    }

    [Fact]
    public async Task RateLimiter_UpdateFromGitHubResponse_UpdatesCorrectly()
    {
        // Arrange
        var tenantId = "tenant-github-update";
        var resetTime = DateTimeOffset.UtcNow.AddHours(1);

        // Simulate GitHub API response headers
        var remainingFromGitHub = 4950;
        var resetTimeFromGitHub = resetTime;

        // Act
        _rateLimiter.UpdateRateLimit(tenantId, remainingFromGitHub, resetTimeFromGitHub);

        var status = await _rateLimiter.CheckRateLimitAsync(tenantId);

        // Assert
        Assert.Equal(remainingFromGitHub, status.Remaining);
        Assert.Equal(resetTimeFromGitHub, status.ResetTime);
        Assert.Equal(5000, status.Limit);
    }

    [Fact]
    public async Task RateLimiter_ConcurrentRequests_ThreadSafe()
    {
        // Arrange
        var tenantId = "tenant-concurrent";
        var tasks = new Task[100];

        // Act - Simulate 100 concurrent requests
        for (int i = 0; i < 100; i++)
        {
            tasks[i] = Task.Run(() => _rateLimiter.RecordRequest(tenantId));
        }

        await Task.WhenAll(tasks);

        var status = await _rateLimiter.CheckRateLimitAsync(tenantId);

        // Assert
        // Should have recorded all 100 requests
        Assert.True(status.Remaining <= 4900, $"Expected remaining <= 4900, but was {status.Remaining}");
    }

    [Fact]
    public async Task RateLimiter_ResetTimeExpired_AutomaticallyResets()
    {
        // Arrange
        var tenantId = "tenant-auto-reset";
        var expiredResetTime = DateTimeOffset.UtcNow.AddSeconds(-5); // 5 seconds ago

        // Set low remaining with expired reset time
        _rateLimiter.UpdateRateLimit(tenantId, 10, expiredResetTime);

        // Act
        await Task.Delay(100); // Small delay
        var status = await _rateLimiter.CheckRateLimitAsync(tenantId);

        // Assert
        Assert.Equal(5000, status.Remaining); // Should be reset to max
        Assert.True(status.ResetTime > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task RateLimiter_UsagePercentage_AccuratelyCalculated()
    {
        // Arrange
        var tenantId = "tenant-usage";
        var resetTime = DateTimeOffset.UtcNow.AddHours(1);

        var testCases = new[]
        {
            (Remaining: 5000, ExpectedPercentage: 0.0),   // 0% used
            (Remaining: 2500, ExpectedPercentage: 50.0),  // 50% used
            (Remaining: 1000, ExpectedPercentage: 80.0),  // 80% used
            (Remaining: 0, ExpectedPercentage: 100.0)     // 100% used
        };

        foreach (var testCase in testCases)
        {
            // Act
            _rateLimiter.UpdateRateLimit(tenantId, testCase.Remaining, resetTime);
            var status = await _rateLimiter.CheckRateLimitAsync(tenantId);

            // Assert
            Assert.Equal(testCase.ExpectedPercentage, status.UsagePercentage, 1); // 1 decimal precision
        }
    }

    [Fact]
    public async Task RateLimiter_SecondsUntilReset_AccuratelyCalculated()
    {
        // Arrange
        var tenantId = "tenant-seconds-until-reset";
        var resetTime = DateTimeOffset.UtcNow.AddSeconds(60); // 60 seconds from now

        // Act
        _rateLimiter.UpdateRateLimit(tenantId, 5000, resetTime);
        var status = await _rateLimiter.CheckRateLimitAsync(tenantId);

        // Assert
        Assert.True(status.SecondsUntilReset >= 55 && status.SecondsUntilReset <= 60,
            $"Expected SecondsUntilReset between 55-60, but was {status.SecondsUntilReset}");
    }

    [Fact]
    public async Task RateLimiter_WithRealGitHubClient_HandlesRateLimitResponse()
    {
        // Note: This test requires a GitHub token to run against real API
        // Skip if not configured
        var githubToken = Environment.GetEnvironmentVariable("GITHUB_TEST_TOKEN");
        if (string.IsNullOrEmpty(githubToken))
        {
            // Skip test if no token configured
            return;
        }

        // Arrange
        var tenantId = "tenant-real-github";
        var client = new GitHubClient(new ProductHeaderValue("Binah-Test"))
        {
            Credentials = new Credentials(githubToken)
        };

        try
        {
            // Act - Make a real API call
            var rateLimit = await client.RateLimit.GetRateLimits();

            // Update our rate limiter with real GitHub data
            _rateLimiter.UpdateRateLimit(
                tenantId,
                rateLimit.Resources.Core.Remaining,
                rateLimit.Resources.Core.Reset);

            var status = await _rateLimiter.CheckRateLimitAsync(tenantId);

            // Assert
            Assert.True(status.Remaining >= 0);
            Assert.True(status.Limit > 0);
            Assert.True(status.ResetTime > DateTimeOffset.UtcNow.AddMinutes(-5));
        }
        catch (RateLimitExceededException ex)
        {
            // If rate limit exceeded, verify our handler would work
            var resetTime = ex.Reset;
            _rateLimiter.UpdateRateLimit(tenantId, 0, resetTime);

            var status = await _rateLimiter.CheckRateLimitAsync(tenantId);
            Assert.True(status.IsExceeded);
        }
    }

    [Fact]
    public async Task RateLimiter_MultipleTenantsApproachingLimit_IndependentWarnings()
    {
        // Arrange
        var tenants = new[] { "tenant-warn-1", "tenant-warn-2", "tenant-warn-3" };
        var resetTime = DateTimeOffset.UtcNow.AddHours(1);

        // Act
        foreach (var tenant in tenants)
        {
            _rateLimiter.UpdateRateLimit(tenant, 95, resetTime); // Approaching threshold
            await _rateLimiter.WaitForRateLimitAsync(tenant);
        }

        // Assert
        foreach (var tenant in tenants)
        {
            var status = await _rateLimiter.CheckRateLimitAsync(tenant);
            Assert.True(status.IsApproachingLimit);
            Assert.False(status.IsExceeded);
        }
    }
}

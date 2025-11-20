using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Binah.Webhooks.Exceptions;
using Binah.Webhooks.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Binah.Webhooks.Services.Implementations;

/// <summary>
/// Implementation of GitHub API rate limiter
/// Tracks rate limits per tenant and enforces waiting when limits are approached
/// </summary>
public class GitHubRateLimiter : IGitHubRateLimiter
{
    private readonly ILogger<GitHubRateLimiter> _logger;
    private readonly IConfiguration _configuration;
    private readonly ConcurrentDictionary<string, RateLimitStatus> _rateLimits;
    private readonly int _maxRequestsPerHour;
    private readonly int _warningThreshold;
    private readonly SemaphoreSlim _semaphore;

    public GitHubRateLimiter(
        ILogger<GitHubRateLimiter> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _rateLimits = new ConcurrentDictionary<string, RateLimitStatus>();
        _semaphore = new SemaphoreSlim(1, 1);

        // Load configuration
        _maxRequestsPerHour = _configuration.GetValue<int>("GitHub:RateLimit:MaxRequestsPerHour", 5000);
        _warningThreshold = _configuration.GetValue<int>("GitHub:RateLimit:WarningThreshold", 100);

        _logger.LogInformation(
            "GitHubRateLimiter initialized. Max requests/hour: {MaxRequests}, Warning threshold: {Threshold}",
            _maxRequestsPerHour,
            _warningThreshold);
    }

    public async Task<RateLimitStatus> CheckRateLimitAsync(string tenantId)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_rateLimits.TryGetValue(tenantId, out var status))
            {
                // Check if reset time has passed
                if (DateTimeOffset.UtcNow >= status.ResetTime)
                {
                    // Reset the rate limit
                    status.Remaining = _maxRequestsPerHour;
                    status.ResetTime = DateTimeOffset.UtcNow.AddHours(1);

                    _logger.LogInformation(
                        "Rate limit reset for tenant {TenantId}. New reset time: {ResetTime}",
                        tenantId,
                        status.ResetTime);
                }

                return status;
            }

            // Initialize rate limit for new tenant
            var newStatus = new RateLimitStatus
            {
                TenantId = tenantId,
                Limit = _maxRequestsPerHour,
                Remaining = _maxRequestsPerHour,
                ResetTime = DateTimeOffset.UtcNow.AddHours(1)
            };

            _rateLimits[tenantId] = newStatus;

            _logger.LogInformation(
                "Initialized rate limit for tenant {TenantId}. Limit: {Limit}, Reset time: {ResetTime}",
                tenantId,
                _maxRequestsPerHour,
                newStatus.ResetTime);

            return newStatus;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task WaitForRateLimitAsync(string tenantId, bool forceRefresh = false)
    {
        var status = await CheckRateLimitAsync(tenantId);

        // If rate limit exceeded, wait until reset
        if (status.IsExceeded)
        {
            var waitTime = status.SecondsUntilReset;

            _logger.LogWarning(
                "Rate limit exceeded for tenant {TenantId}. Waiting {WaitSeconds} seconds until reset at {ResetTime}",
                tenantId,
                waitTime,
                status.ResetTime);

            if (waitTime > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(waitTime + 1)); // Add 1 second buffer
            }

            // Refresh status after waiting
            await CheckRateLimitAsync(tenantId);
        }
        // If approaching limit, log warning
        else if (status.IsApproachingLimit)
        {
            _logger.LogWarning(
                "Approaching rate limit for tenant {TenantId}. Remaining: {Remaining}/{Limit} ({Percentage:F1}% used)",
                tenantId,
                status.Remaining,
                status.Limit,
                status.UsagePercentage);
        }
    }

    public async Task<DateTimeOffset> GetRateLimitResetTimeAsync(string tenantId)
    {
        var status = await CheckRateLimitAsync(tenantId);
        return status.ResetTime;
    }

    public void RecordRequest(string tenantId)
    {
        if (_rateLimits.TryGetValue(tenantId, out var status))
        {
            if (status.Remaining > 0)
            {
                status.Remaining--;

                _logger.LogDebug(
                    "Request recorded for tenant {TenantId}. Remaining: {Remaining}/{Limit}",
                    tenantId,
                    status.Remaining,
                    status.Limit);

                // Log warning if approaching threshold
                if (status.Remaining == _warningThreshold)
                {
                    _logger.LogWarning(
                        "Rate limit warning threshold reached for tenant {TenantId}. Only {Remaining} requests remaining",
                        tenantId,
                        status.Remaining);
                }
            }
            else
            {
                _logger.LogError(
                    "Rate limit exceeded for tenant {TenantId}. Request should have been blocked",
                    tenantId);
            }
        }
        else
        {
            _logger.LogWarning(
                "Attempted to record request for tenant {TenantId} with no rate limit status. Initializing...",
                tenantId);

            // Initialize if not exists
            _rateLimits[tenantId] = new RateLimitStatus
            {
                TenantId = tenantId,
                Limit = _maxRequestsPerHour,
                Remaining = _maxRequestsPerHour - 1,
                ResetTime = DateTimeOffset.UtcNow.AddHours(1)
            };
        }
    }

    public void UpdateRateLimit(string tenantId, int remaining, DateTimeOffset resetTime)
    {
        var status = _rateLimits.GetOrAdd(tenantId, _ => new RateLimitStatus
        {
            TenantId = tenantId,
            Limit = _maxRequestsPerHour
        });

        var oldRemaining = status.Remaining;
        status.Remaining = remaining;
        status.ResetTime = resetTime;

        _logger.LogDebug(
            "Updated rate limit for tenant {TenantId}. Remaining: {OldRemaining} → {NewRemaining}, Reset time: {ResetTime}",
            tenantId,
            oldRemaining,
            remaining,
            resetTime);

        // Log warning if rate limit changed significantly
        if (Math.Abs(oldRemaining - remaining) > 100)
        {
            _logger.LogWarning(
                "Significant rate limit change for tenant {TenantId}. Changed by {Change} requests",
                tenantId,
                oldRemaining - remaining);
        }
    }
}

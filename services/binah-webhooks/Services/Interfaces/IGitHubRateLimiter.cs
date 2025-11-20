using System;
using System.Threading.Tasks;

namespace Binah.Webhooks.Services.Interfaces;

/// <summary>
/// Service for managing GitHub API rate limits
/// GitHub allows 5,000 requests per hour for authenticated users
/// </summary>
public interface IGitHubRateLimiter
{
    /// <summary>
    /// Check current rate limit status for a tenant
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <returns>Rate limit status including remaining requests and reset time</returns>
    Task<RateLimitStatus> CheckRateLimitAsync(string tenantId);

    /// <summary>
    /// Wait if rate limit is exceeded or approaching limit
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="forceRefresh">Force refresh rate limit from GitHub API</param>
    /// <returns>Task that completes when safe to proceed</returns>
    Task WaitForRateLimitAsync(string tenantId, bool forceRefresh = false);

    /// <summary>
    /// Get the UTC time when the rate limit will reset
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <returns>Reset time in UTC</returns>
    Task<DateTimeOffset> GetRateLimitResetTimeAsync(string tenantId);

    /// <summary>
    /// Record that a request was made (decrement remaining count)
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    void RecordRequest(string tenantId);

    /// <summary>
    /// Update rate limit information from GitHub API response headers
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="remaining">Remaining requests</param>
    /// <param name="resetTime">Reset time</param>
    void UpdateRateLimit(string tenantId, int remaining, DateTimeOffset resetTime);
}

/// <summary>
/// Represents the current rate limit status for a tenant
/// </summary>
public class RateLimitStatus
{
    /// <summary>
    /// Tenant ID
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Maximum requests allowed per hour
    /// </summary>
    public int Limit { get; set; }

    /// <summary>
    /// Remaining requests before rate limit
    /// </summary>
    public int Remaining { get; set; }

    /// <summary>
    /// UTC time when rate limit resets
    /// </summary>
    public DateTimeOffset ResetTime { get; set; }

    /// <summary>
    /// Seconds until rate limit resets
    /// </summary>
    public int SecondsUntilReset => (int)(ResetTime - DateTimeOffset.UtcNow).TotalSeconds;

    /// <summary>
    /// Whether the rate limit is currently exceeded
    /// </summary>
    public bool IsExceeded => Remaining <= 0;

    /// <summary>
    /// Whether approaching rate limit threshold (< 100 requests remaining)
    /// </summary>
    public bool IsApproachingLimit => Remaining < 100;

    /// <summary>
    /// Percentage of rate limit used
    /// </summary>
    public double UsagePercentage => Limit > 0 ? ((double)(Limit - Remaining) / Limit) * 100 : 0;
}

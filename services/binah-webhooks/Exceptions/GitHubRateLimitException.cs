using System;
using System.Net;

namespace Binah.Webhooks.Exceptions;

/// <summary>
/// Exception thrown when GitHub API rate limit is exceeded (HTTP 403)
/// </summary>
public class GitHubRateLimitException : GitHubApiException
{
    /// <summary>
    /// Number of requests remaining before rate limit reset
    /// </summary>
    public int RemainingRequests { get; }

    /// <summary>
    /// UTC timestamp when the rate limit will reset
    /// </summary>
    public DateTimeOffset ResetTime { get; }

    /// <summary>
    /// Seconds until the rate limit resets
    /// </summary>
    public int SecondsUntilReset => (int)(ResetTime - DateTimeOffset.UtcNow).TotalSeconds;

    public GitHubRateLimitException(
        string message,
        int remainingRequests,
        DateTimeOffset resetTime,
        string? correlationId = null,
        string? gitHubMessage = null)
        : base(message, HttpStatusCode.Forbidden, correlationId, gitHubMessage)
    {
        RemainingRequests = remainingRequests;
        ResetTime = resetTime;
    }

    public GitHubRateLimitException(
        string message,
        int remainingRequests,
        DateTimeOffset resetTime,
        Exception innerException,
        string? correlationId = null,
        string? gitHubMessage = null)
        : base(message, HttpStatusCode.Forbidden, innerException, correlationId, gitHubMessage)
    {
        RemainingRequests = remainingRequests;
        ResetTime = resetTime;
    }

    public override string ToString()
    {
        var baseMessage = base.ToString();
        return $"{baseMessage}\nRemaining Requests: {RemainingRequests}\nReset Time: {ResetTime:yyyy-MM-dd HH:mm:ss UTC}\nSeconds Until Reset: {SecondsUntilReset}";
    }
}

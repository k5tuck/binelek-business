using System;
using System.Net;

namespace Binah.Webhooks.Exceptions;

/// <summary>
/// Base exception for all GitHub API errors
/// </summary>
public class GitHubApiException : Exception
{
    /// <summary>
    /// HTTP status code returned by GitHub API
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// Correlation ID for tracing the request
    /// </summary>
    public string? CorrelationId { get; }

    /// <summary>
    /// GitHub error message
    /// </summary>
    public string? GitHubMessage { get; }

    public GitHubApiException(string message, HttpStatusCode statusCode, string? correlationId = null, string? gitHubMessage = null)
        : base(message)
    {
        StatusCode = statusCode;
        CorrelationId = correlationId;
        GitHubMessage = gitHubMessage;
    }

    public GitHubApiException(string message, HttpStatusCode statusCode, Exception innerException, string? correlationId = null, string? gitHubMessage = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        CorrelationId = correlationId;
        GitHubMessage = gitHubMessage;
    }

    public override string ToString()
    {
        var baseMessage = base.ToString();
        return $"{baseMessage}\nHTTP Status: {(int)StatusCode} {StatusCode}\nCorrelation ID: {CorrelationId ?? "N/A"}\nGitHub Message: {GitHubMessage ?? "N/A"}";
    }
}

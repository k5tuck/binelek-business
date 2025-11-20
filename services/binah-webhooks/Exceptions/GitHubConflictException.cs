using System;
using System.Net;

namespace Binah.Webhooks.Exceptions;

/// <summary>
/// Exception thrown when GitHub API operation results in a conflict (HTTP 409)
/// Common for merge conflicts or when resource already exists
/// </summary>
public class GitHubConflictException : GitHubApiException
{
    /// <summary>
    /// Type of conflict (e.g., "merge_conflict", "already_exists", "concurrent_update")
    /// </summary>
    public string? ConflictType { get; }

    /// <summary>
    /// Detailed conflict information from GitHub
    /// </summary>
    public string? ConflictDetails { get; }

    public GitHubConflictException(
        string message,
        string? conflictType = null,
        string? conflictDetails = null,
        string? correlationId = null,
        string? gitHubMessage = null)
        : base(message, HttpStatusCode.Conflict, correlationId, gitHubMessage)
    {
        ConflictType = conflictType;
        ConflictDetails = conflictDetails;
    }

    public GitHubConflictException(
        string message,
        Exception innerException,
        string? conflictType = null,
        string? conflictDetails = null,
        string? correlationId = null,
        string? gitHubMessage = null)
        : base(message, HttpStatusCode.Conflict, innerException, correlationId, gitHubMessage)
    {
        ConflictType = conflictType;
        ConflictDetails = conflictDetails;
    }

    public override string ToString()
    {
        var baseMessage = base.ToString();
        return $"{baseMessage}\nConflict Type: {ConflictType ?? "N/A"}\nConflict Details: {ConflictDetails ?? "N/A"}";
    }
}

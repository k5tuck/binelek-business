using System;
using System.Net;

namespace Binah.Webhooks.Exceptions;

/// <summary>
/// Exception thrown when GitHub API resource is not found (HTTP 404)
/// Indicates repository, branch, file, or PR doesn't exist
/// </summary>
public class GitHubNotFoundException : GitHubApiException
{
    /// <summary>
    /// Type of resource that was not found (e.g., "repository", "branch", "file", "pull_request")
    /// </summary>
    public string? ResourceType { get; }

    /// <summary>
    /// Identifier of the resource (e.g., "owner/repo", "branch-name", "file/path.cs")
    /// </summary>
    public string? ResourceIdentifier { get; }

    public GitHubNotFoundException(
        string message,
        string? resourceType = null,
        string? resourceIdentifier = null,
        string? correlationId = null,
        string? gitHubMessage = null)
        : base(message, HttpStatusCode.NotFound, correlationId, gitHubMessage)
    {
        ResourceType = resourceType;
        ResourceIdentifier = resourceIdentifier;
    }

    public GitHubNotFoundException(
        string message,
        Exception innerException,
        string? resourceType = null,
        string? resourceIdentifier = null,
        string? correlationId = null,
        string? gitHubMessage = null)
        : base(message, HttpStatusCode.NotFound, innerException, correlationId, gitHubMessage)
    {
        ResourceType = resourceType;
        ResourceIdentifier = resourceIdentifier;
    }

    public override string ToString()
    {
        var baseMessage = base.ToString();
        return $"{baseMessage}\nResource Type: {ResourceType ?? "N/A"}\nResource Identifier: {ResourceIdentifier ?? "N/A"}";
    }
}

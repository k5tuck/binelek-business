using System;
using System.Net;

namespace Binah.Webhooks.Exceptions;

/// <summary>
/// Exception thrown when GitHub API authentication fails (HTTP 401)
/// Indicates invalid or expired credentials
/// </summary>
public class GitHubAuthenticationException : GitHubApiException
{
    /// <summary>
    /// Tenant ID that failed authentication
    /// </summary>
    public string? TenantId { get; }

    public GitHubAuthenticationException(
        string message,
        string? tenantId = null,
        string? correlationId = null,
        string? gitHubMessage = null)
        : base(message, HttpStatusCode.Unauthorized, correlationId, gitHubMessage)
    {
        TenantId = tenantId;
    }

    public GitHubAuthenticationException(
        string message,
        Exception innerException,
        string? tenantId = null,
        string? correlationId = null,
        string? gitHubMessage = null)
        : base(message, HttpStatusCode.Unauthorized, innerException, correlationId, gitHubMessage)
    {
        TenantId = tenantId;
    }

    public override string ToString()
    {
        var baseMessage = base.ToString();
        return $"{baseMessage}\nTenant ID: {TenantId ?? "N/A"}";
    }
}

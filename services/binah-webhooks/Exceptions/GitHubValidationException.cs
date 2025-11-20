using System;
using System.Collections.Generic;
using System.Net;

namespace Binah.Webhooks.Exceptions;

/// <summary>
/// Exception thrown when GitHub API request validation fails (HTTP 422)
/// Indicates invalid request parameters or data
/// </summary>
public class GitHubValidationException : GitHubApiException
{
    /// <summary>
    /// List of validation errors from GitHub
    /// </summary>
    public IReadOnlyList<ValidationError> ValidationErrors { get; }

    public GitHubValidationException(
        string message,
        IEnumerable<ValidationError>? validationErrors = null,
        string? correlationId = null,
        string? gitHubMessage = null)
        : base(message, (HttpStatusCode)422, correlationId, gitHubMessage)
    {
        ValidationErrors = (validationErrors ?? new List<ValidationError>()).ToList().AsReadOnly();
    }

    public GitHubValidationException(
        string message,
        Exception innerException,
        IEnumerable<ValidationError>? validationErrors = null,
        string? correlationId = null,
        string? gitHubMessage = null)
        : base(message, (HttpStatusCode)422, innerException, correlationId, gitHubMessage)
    {
        ValidationErrors = (validationErrors ?? new List<ValidationError>()).ToList().AsReadOnly();
    }

    public override string ToString()
    {
        var baseMessage = base.ToString();
        var errorsList = ValidationErrors.Count > 0
            ? string.Join("\n  - ", ValidationErrors.Select(e => $"{e.Field}: {e.Message}"))
            : "No validation errors provided";
        return $"{baseMessage}\nValidation Errors:\n  - {errorsList}";
    }
}

/// <summary>
/// Represents a single validation error from GitHub API
/// </summary>
public class ValidationError
{
    /// <summary>
    /// The field that failed validation
    /// </summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// Error code (e.g., "missing", "invalid", "already_exists")
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable error message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"{Field} ({Code}): {Message}";
    }
}

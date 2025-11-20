using System.Text.Json;
using Binah.Webhooks.Models.DTOs.GitHub;
using Binah.Webhooks.Services.Interfaces;

namespace Binah.Webhooks.Services.Implementations;

/// <summary>
/// Implementation of GitHub event parser service
/// Parses GitHub webhook payloads into strongly-typed DTOs
/// </summary>
public class GitHubEventParser : IGitHubEventParser
{
    private readonly ILogger<GitHubEventParser> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    // Supported GitHub event types
    private static readonly string[] SupportedEventTypes = new[]
    {
        "push",
        "pull_request",
        "issues",
        "issue_comment",
        "pull_request_review"
    };

    /// <summary>
    /// Initializes a new instance of the GitHubEventParser
    /// </summary>
    /// <param name="logger">Logger instance</param>
    public GitHubEventParser(ILogger<GitHubEventParser> logger)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };
    }

    /// <inheritdoc />
    public T ParseEvent<T>(string eventType, string payload) where T : GitHubWebhookEventDto
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new ArgumentNullException(nameof(eventType), "Event type cannot be null or empty");
        }

        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new ArgumentNullException(nameof(payload), "Payload cannot be null or empty");
        }

        _logger.LogDebug("Parsing GitHub event: {EventType}", eventType);

        try
        {
            // Verify that the event type matches the expected DTO type
            ValidateEventTypeMatch<T>(eventType);

            // Deserialize the payload
            var parsedEvent = JsonSerializer.Deserialize<T>(payload, _jsonOptions);

            if (parsedEvent == null)
            {
                throw new InvalidOperationException($"Failed to deserialize event payload for type {eventType}");
            }

            // Validate required fields
            ValidateParsedEvent(parsedEvent, eventType);

            _logger.LogInformation("Successfully parsed GitHub {EventType} event", eventType);

            return parsedEvent;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse GitHub event payload: Invalid JSON");
            throw new JsonException($"Invalid JSON in GitHub {eventType} event payload", ex);
        }
        catch (Exception ex) when (ex is not ArgumentException && ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Unexpected error parsing GitHub event");
            throw new InvalidOperationException($"Failed to parse GitHub {eventType} event", ex);
        }
    }

    /// <inheritdoc />
    public GitHubWebhookEventDto ParseEvent(string eventType, string payload)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new ArgumentNullException(nameof(eventType), "Event type cannot be null or empty");
        }

        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new ArgumentNullException(nameof(payload), "Payload cannot be null or empty");
        }

        // Check if event type is supported
        if (!SupportedEventTypes.Contains(eventType.ToLowerInvariant()))
        {
            _logger.LogWarning("Unsupported GitHub event type: {EventType}", eventType);
            throw new NotSupportedException($"GitHub event type '{eventType}' is not supported. Supported types: {string.Join(", ", SupportedEventTypes)}");
        }

        _logger.LogDebug("Parsing GitHub event (untyped): {EventType}", eventType);

        // Parse based on event type
        return eventType.ToLowerInvariant() switch
        {
            "push" => ParseEvent<PushEventDto>(eventType, payload),
            "pull_request" => ParseEvent<PullRequestEventDto>(eventType, payload),
            "issues" => ParseEvent<IssuesEventDto>(eventType, payload),
            "issue_comment" => ParseEvent<IssueCommentEventDto>(eventType, payload),
            "pull_request_review" => ParseEvent<PullRequestReviewEventDto>(eventType, payload),
            _ => throw new NotSupportedException($"GitHub event type '{eventType}' is not supported")
        };
    }

    /// <inheritdoc />
    public bool ValidateEventPayload(string eventType, string payload)
    {
        try
        {
            // Attempt to parse the event
            ParseEvent(eventType, payload);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Event payload validation failed for {EventType}", eventType);
            return false;
        }
    }

    /// <inheritdoc />
    public string[] GetSupportedEventTypes()
    {
        return SupportedEventTypes.ToArray();
    }

    /// <summary>
    /// Validate that the event type matches the expected DTO type
    /// </summary>
    private void ValidateEventTypeMatch<T>(string eventType) where T : GitHubWebhookEventDto
    {
        var expectedType = typeof(T);
        var normalizedEventType = eventType.ToLowerInvariant();

        var isValid = (expectedType.Name, normalizedEventType) switch
        {
            (nameof(PushEventDto), "push") => true,
            (nameof(PullRequestEventDto), "pull_request") => true,
            (nameof(IssuesEventDto), "issues") => true,
            (nameof(IssueCommentEventDto), "issue_comment") => true,
            (nameof(PullRequestReviewEventDto), "pull_request_review") => true,
            (nameof(GitHubWebhookEventDto), _) => true, // Base type accepts any supported event
            _ => false
        };

        if (!isValid)
        {
            throw new ArgumentException(
                $"Event type '{eventType}' does not match expected DTO type '{expectedType.Name}'",
                nameof(eventType)
            );
        }
    }

    /// <summary>
    /// Validate that required fields are present in the parsed event
    /// </summary>
    private void ValidateParsedEvent(GitHubWebhookEventDto parsedEvent, string eventType)
    {
        // Validate common required fields
        if (parsedEvent.Repository == null)
        {
            throw new InvalidOperationException($"Missing required field 'repository' in {eventType} event");
        }

        if (parsedEvent.Sender == null)
        {
            throw new InvalidOperationException($"Missing required field 'sender' in {eventType} event");
        }

        // Validate event-specific required fields
        switch (parsedEvent)
        {
            case PushEventDto pushEvent:
                ValidatePushEvent(pushEvent);
                break;

            case PullRequestEventDto prEvent:
                ValidatePullRequestEvent(prEvent);
                break;

            case IssuesEventDto issueEvent:
                ValidateIssuesEvent(issueEvent);
                break;

            case IssueCommentEventDto commentEvent:
                ValidateIssueCommentEvent(commentEvent);
                break;

            case PullRequestReviewEventDto reviewEvent:
                ValidatePullRequestReviewEvent(reviewEvent);
                break;
        }
    }

    /// <summary>
    /// Validate push event specific fields
    /// </summary>
    private void ValidatePushEvent(PushEventDto pushEvent)
    {
        if (string.IsNullOrWhiteSpace(pushEvent.Ref))
        {
            throw new InvalidOperationException("Missing required field 'ref' in push event");
        }

        if (string.IsNullOrWhiteSpace(pushEvent.After))
        {
            throw new InvalidOperationException("Missing required field 'after' in push event");
        }

        if (pushEvent.Commits == null)
        {
            throw new InvalidOperationException("Missing required field 'commits' in push event");
        }
    }

    /// <summary>
    /// Validate pull request event specific fields
    /// </summary>
    private void ValidatePullRequestEvent(PullRequestEventDto prEvent)
    {
        if (prEvent.PullRequest == null)
        {
            throw new InvalidOperationException("Missing required field 'pull_request' in pull_request event");
        }

        if (prEvent.Number <= 0)
        {
            throw new InvalidOperationException("Invalid pull request number in pull_request event");
        }

        if (string.IsNullOrWhiteSpace(prEvent.Action))
        {
            throw new InvalidOperationException("Missing required field 'action' in pull_request event");
        }
    }

    /// <summary>
    /// Validate issues event specific fields
    /// </summary>
    private void ValidateIssuesEvent(IssuesEventDto issueEvent)
    {
        if (issueEvent.Issue == null)
        {
            throw new InvalidOperationException("Missing required field 'issue' in issues event");
        }

        if (string.IsNullOrWhiteSpace(issueEvent.Action))
        {
            throw new InvalidOperationException("Missing required field 'action' in issues event");
        }
    }

    /// <summary>
    /// Validate issue comment event specific fields
    /// </summary>
    private void ValidateIssueCommentEvent(IssueCommentEventDto commentEvent)
    {
        if (commentEvent.Issue == null)
        {
            throw new InvalidOperationException("Missing required field 'issue' in issue_comment event");
        }

        if (commentEvent.Comment == null)
        {
            throw new InvalidOperationException("Missing required field 'comment' in issue_comment event");
        }

        if (string.IsNullOrWhiteSpace(commentEvent.Action))
        {
            throw new InvalidOperationException("Missing required field 'action' in issue_comment event");
        }
    }

    /// <summary>
    /// Validate pull request review event specific fields
    /// </summary>
    private void ValidatePullRequestReviewEvent(PullRequestReviewEventDto reviewEvent)
    {
        if (reviewEvent.PullRequest == null)
        {
            throw new InvalidOperationException("Missing required field 'pull_request' in pull_request_review event");
        }

        if (reviewEvent.Review == null)
        {
            throw new InvalidOperationException("Missing required field 'review' in pull_request_review event");
        }

        if (string.IsNullOrWhiteSpace(reviewEvent.Action))
        {
            throw new InvalidOperationException("Missing required field 'action' in pull_request_review event");
        }
    }
}

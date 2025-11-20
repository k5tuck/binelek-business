# GitHub Event Parser - Sample Usage

This document demonstrates how to use the GitHub Event Parser service to parse GitHub webhook events.

## Overview

The `IGitHubEventParser` service provides strongly-typed parsing of GitHub webhook event payloads into DTOs.

## Supported Event Types

- **push** - Code pushed to a repository
- **pull_request** - Pull request opened, closed, synchronized, etc.
- **issues** - Issue opened, closed, edited, etc.
- **issue_comment** - Comment on issue or pull request
- **pull_request_review** - Pull request review submitted

## Basic Usage

### 1. Inject the Service

```csharp
public class GitHubWebhookController : ControllerBase
{
    private readonly IGitHubEventParser _eventParser;

    public GitHubWebhookController(IGitHubEventParser eventParser)
    {
        _eventParser = eventParser;
    }
}
```

### 2. Parse Typed Events

```csharp
[HttpPost("webhook")]
public async Task<IActionResult> ReceiveWebhook()
{
    // Read the GitHub event type from header
    var eventType = Request.Headers["X-GitHub-Event"].ToString();

    // Read the raw JSON payload
    using var reader = new StreamReader(Request.Body);
    var payload = await reader.ReadToEndAsync();

    // Parse based on event type
    switch (eventType.ToLowerInvariant())
    {
        case "push":
            var pushEvent = _eventParser.ParseEvent<PushEventDto>("push", payload);
            await HandlePushEvent(pushEvent);
            break;

        case "pull_request":
            var prEvent = _eventParser.ParseEvent<PullRequestEventDto>("pull_request", payload);
            await HandlePullRequestEvent(prEvent);
            break;

        case "issues":
            var issueEvent = _eventParser.ParseEvent<IssuesEventDto>("issues", payload);
            await HandleIssuesEvent(issueEvent);
            break;

        case "issue_comment":
            var commentEvent = _eventParser.ParseEvent<IssueCommentEventDto>("issue_comment", payload);
            await HandleIssueCommentEvent(commentEvent);
            break;

        case "pull_request_review":
            var reviewEvent = _eventParser.ParseEvent<PullRequestReviewEventDto>("pull_request_review", payload);
            await HandlePullRequestReviewEvent(reviewEvent);
            break;

        default:
            return BadRequest($"Unsupported event type: {eventType}");
    }

    return Ok();
}
```

### 3. Parse Untyped Events (Dynamic Routing)

```csharp
[HttpPost("webhook/dynamic")]
public async Task<IActionResult> ReceiveWebhookDynamic()
{
    var eventType = Request.Headers["X-GitHub-Event"].ToString();

    using var reader = new StreamReader(Request.Body);
    var payload = await reader.ReadToEndAsync();

    try
    {
        // Parse without specifying type - returns base type
        var webhookEvent = _eventParser.ParseEvent(eventType, payload);

        // Handle based on actual type
        switch (webhookEvent)
        {
            case PushEventDto pushEvent:
                await HandlePushEvent(pushEvent);
                break;

            case PullRequestEventDto prEvent:
                await HandlePullRequestEvent(prEvent);
                break;

            case IssuesEventDto issueEvent:
                await HandleIssuesEvent(issueEvent);
                break;

            default:
                _logger.LogWarning("Unhandled event type: {EventType}", webhookEvent.GetType().Name);
                break;
        }

        return Ok();
    }
    catch (NotSupportedException ex)
    {
        return BadRequest(ex.Message);
    }
}
```

## Handling Specific Events

### Push Event

```csharp
private async Task HandlePushEvent(PushEventDto pushEvent)
{
    _logger.LogInformation(
        "Push to {Branch} in {Repository}: {CommitCount} commits",
        pushEvent.Ref,
        pushEvent.Repository?.FullName,
        pushEvent.Commits.Count
    );

    // Access commit details
    foreach (var commit in pushEvent.Commits)
    {
        _logger.LogInformation(
            "Commit {Sha}: {Message} by {Author}",
            commit.Id,
            commit.Message,
            commit.Author?.Name
        );

        // Files changed
        _logger.LogDebug(
            "  Added: {Added}, Modified: {Modified}, Removed: {Removed}",
            commit.Added.Count,
            commit.Modified.Count,
            commit.Removed.Count
        );
    }

    // Determine if this is a branch creation/deletion
    if (pushEvent.Created)
    {
        await HandleBranchCreated(pushEvent);
    }
    else if (pushEvent.Deleted)
    {
        await HandleBranchDeleted(pushEvent);
    }
}
```

### Pull Request Event

```csharp
private async Task HandlePullRequestEvent(PullRequestEventDto prEvent)
{
    var action = prEvent.Action; // opened, closed, synchronize, etc.
    var pr = prEvent.PullRequest;

    _logger.LogInformation(
        "Pull Request {Action}: #{Number} - {Title} in {Repository}",
        action,
        pr?.Number,
        pr?.Title,
        prEvent.Repository?.FullName
    );

    switch (action?.ToLowerInvariant())
    {
        case "opened":
            await HandlePROpened(prEvent);
            break;

        case "closed":
            if (pr?.Merged == true)
            {
                await HandlePRMerged(prEvent);
            }
            else
            {
                await HandlePRClosed(prEvent);
            }
            break;

        case "synchronize":
            await HandlePRSynchronized(prEvent);
            break;

        case "reopened":
            await HandlePRReopened(prEvent);
            break;
    }

    // Access PR details
    if (pr != null)
    {
        _logger.LogDebug(
            "PR Stats: {Commits} commits, +{Additions}/-{Deletions}, {Files} files changed",
            pr.Commits,
            pr.Additions,
            pr.Deletions,
            pr.ChangedFiles
        );

        _logger.LogDebug(
            "Branches: {Head} -> {Base}",
            pr.Head?.Ref,
            pr.Base?.Ref
        );
    }
}
```

### Issues Event

```csharp
private async Task HandleIssuesEvent(IssuesEventDto issueEvent)
{
    var action = issueEvent.Action; // opened, closed, edited, etc.
    var issue = issueEvent.Issue;

    _logger.LogInformation(
        "Issue {Action}: #{Number} - {Title}",
        action,
        issue?.Number,
        issue?.Title
    );

    switch (action?.ToLowerInvariant())
    {
        case "opened":
            await NotifyNewIssue(issueEvent);
            break;

        case "closed":
            await HandleIssueClosed(issueEvent);
            break;

        case "labeled":
            await HandleIssueLabeled(issueEvent);
            break;

        case "assigned":
            await HandleIssueAssigned(issueEvent);
            break;
    }
}
```

### Issue Comment Event

```csharp
private async Task HandleIssueCommentEvent(IssueCommentEventDto commentEvent)
{
    var action = commentEvent.Action; // created, edited, deleted
    var comment = commentEvent.Comment;
    var issue = commentEvent.Issue;

    _logger.LogInformation(
        "Comment {Action} on Issue #{Number} by {User}",
        action,
        issue?.Number,
        comment?.User?.Login
    );

    if (action == "created")
    {
        // Check for bot commands in comment
        var body = comment?.Body ?? "";
        if (body.StartsWith("/"))
        {
            await HandleBotCommand(body, commentEvent);
        }

        // Notify issue participants
        await NotifyIssueParticipants(commentEvent);
    }
}
```

### Pull Request Review Event

```csharp
private async Task HandlePullRequestReviewEvent(PullRequestReviewEventDto reviewEvent)
{
    var action = reviewEvent.Action; // submitted, edited, dismissed
    var review = reviewEvent.Review;
    var pr = reviewEvent.PullRequest;

    _logger.LogInformation(
        "PR Review {Action}: {State} on PR #{Number} by {Reviewer}",
        action,
        review?.State,
        pr?.Number,
        review?.User?.Login
    );

    // Handle review states
    switch (review?.State?.ToUpperInvariant())
    {
        case "APPROVED":
            await HandleReviewApproved(reviewEvent);
            break;

        case "CHANGES_REQUESTED":
            await HandleChangesRequested(reviewEvent);
            break;

        case "COMMENTED":
            await HandleReviewCommented(reviewEvent);
            break;
    }
}
```

## Error Handling

```csharp
public async Task<IActionResult> ReceiveWebhookWithErrorHandling()
{
    var eventType = Request.Headers["X-GitHub-Event"].ToString();

    using var reader = new StreamReader(Request.Body);
    var payload = await reader.ReadToEndAsync();

    try
    {
        var webhookEvent = _eventParser.ParseEvent(eventType, payload);
        await ProcessEvent(webhookEvent);
        return Ok();
    }
    catch (ArgumentNullException ex)
    {
        _logger.LogError(ex, "Invalid input: {Message}", ex.Message);
        return BadRequest("Event type or payload is missing");
    }
    catch (ArgumentException ex)
    {
        _logger.LogError(ex, "Event type mismatch: {Message}", ex.Message);
        return BadRequest("Event type does not match payload structure");
    }
    catch (JsonException ex)
    {
        _logger.LogError(ex, "Invalid JSON payload: {Message}", ex.Message);
        return BadRequest("Invalid JSON in webhook payload");
    }
    catch (InvalidOperationException ex)
    {
        _logger.LogError(ex, "Missing required fields: {Message}", ex.Message);
        return BadRequest("Required fields missing from payload");
    }
    catch (NotSupportedException ex)
    {
        _logger.LogWarning(ex, "Unsupported event type: {EventType}", eventType);
        return BadRequest($"Event type '{eventType}' is not supported");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error processing webhook");
        return StatusCode(500, "Internal server error");
    }
}
```

## Validation

```csharp
public async Task<IActionResult> ValidateWebhookPayload()
{
    var eventType = Request.Headers["X-GitHub-Event"].ToString();

    using var reader = new StreamReader(Request.Body);
    var payload = await reader.ReadToEndAsync();

    // Validate before processing
    if (!_eventParser.ValidateEventPayload(eventType, payload))
    {
        _logger.LogWarning("Invalid webhook payload for event type: {EventType}", eventType);
        return BadRequest("Invalid webhook payload");
    }

    // Payload is valid, proceed with processing
    var webhookEvent = _eventParser.ParseEvent(eventType, payload);
    await ProcessEvent(webhookEvent);

    return Ok();
}
```

## Getting Supported Event Types

```csharp
[HttpGet("supported-events")]
public IActionResult GetSupportedEventTypes()
{
    var supportedTypes = _eventParser.GetSupportedEventTypes();
    return Ok(new
    {
        supported_event_types = supportedTypes,
        count = supportedTypes.Length
    });
}
```

## Complete Example: Webhook Controller

```csharp
using Binah.Webhooks.Models.DTOs.GitHub;
using Binah.Webhooks.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Binah.Webhooks.Controllers;

[ApiController]
[Route("api/github")]
public class GitHubWebhookController : ControllerBase
{
    private readonly IGitHubEventParser _eventParser;
    private readonly ILogger<GitHubWebhookController> _logger;

    public GitHubWebhookController(
        IGitHubEventParser eventParser,
        ILogger<GitHubWebhookController> logger)
    {
        _eventParser = eventParser;
        _logger = logger;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> ReceiveWebhook()
    {
        // Get event type from header
        var eventType = Request.Headers["X-GitHub-Event"].ToString();

        if (string.IsNullOrEmpty(eventType))
        {
            return BadRequest("X-GitHub-Event header is missing");
        }

        // Read payload
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync();

        try
        {
            // Parse event
            var webhookEvent = _eventParser.ParseEvent(eventType, payload);

            _logger.LogInformation(
                "Received GitHub {EventType} event from {Repository}",
                eventType,
                webhookEvent.Repository?.FullName
            );

            // Route to appropriate handler
            await RouteEvent(eventType, webhookEvent);

            return Ok(new { message = "Webhook processed successfully" });
        }
        catch (NotSupportedException ex)
        {
            _logger.LogWarning(ex, "Unsupported event type: {EventType}", eventType);
            return BadRequest(new { error = $"Event type '{eventType}' is not supported" });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON in webhook payload");
            return BadRequest(new { error = "Invalid JSON payload" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    private async Task RouteEvent(string eventType, GitHubWebhookEventDto webhookEvent)
    {
        switch (webhookEvent)
        {
            case PushEventDto pushEvent:
                await HandlePushEvent(pushEvent);
                break;

            case PullRequestEventDto prEvent:
                await HandlePullRequestEvent(prEvent);
                break;

            case IssuesEventDto issueEvent:
                await HandleIssuesEvent(issueEvent);
                break;

            case IssueCommentEventDto commentEvent:
                await HandleIssueCommentEvent(commentEvent);
                break;

            case PullRequestReviewEventDto reviewEvent:
                await HandlePullRequestReviewEvent(reviewEvent);
                break;

            default:
                _logger.LogWarning("No handler for event type: {EventType}", eventType);
                break;
        }
    }

    private Task HandlePushEvent(PushEventDto pushEvent)
    {
        // Implementation
        return Task.CompletedTask;
    }

    private Task HandlePullRequestEvent(PullRequestEventDto prEvent)
    {
        // Implementation
        return Task.CompletedTask;
    }

    private Task HandleIssuesEvent(IssuesEventDto issueEvent)
    {
        // Implementation
        return Task.CompletedTask;
    }

    private Task HandleIssueCommentEvent(IssueCommentEventDto commentEvent)
    {
        // Implementation
        return Task.CompletedTask;
    }

    private Task HandlePullRequestReviewEvent(PullRequestReviewEventDto reviewEvent)
    {
        // Implementation
        return Task.CompletedTask;
    }
}
```

## Testing

See `Tests/GitHubEventParserTests.cs` for comprehensive unit tests covering:
- Valid event parsing for all event types
- Error handling (null inputs, invalid JSON, missing required fields)
- Event type validation
- Untyped event parsing

## Next Steps

After implementing the parser, you can:
1. Store parsed events in PostgreSQL
2. Publish events to Kafka for downstream processing
3. Implement signature verification (HMAC-SHA256)
4. Add rate limiting per repository
5. Create webhook delivery tracking

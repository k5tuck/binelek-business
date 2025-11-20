# GitHub Pull Request Service - Usage Examples

This document provides comprehensive examples of how to use the GitHub Pull Request service in the Binah.Webhooks service.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Basic PR Creation](#basic-pr-creation)
- [Advanced PR Creation with Templates](#advanced-pr-creation-with-templates)
- [PR Management](#pr-management)
- [PR Status Checking](#pr-status-checking)
- [Integration Examples](#integration-examples)
- [Error Handling](#error-handling)

---

## Prerequisites

### 1. OAuth Token Setup

Before creating pull requests, ensure the tenant has a GitHub OAuth token stored:

```csharp
// Store OAuth token for tenant
var token = new GitHubOAuthToken
{
    TenantId = tenantId,
    AccessToken = "ghp_your_personal_access_token",
    TokenType = "Bearer",
    Scope = "repo",
    CreatedAt = DateTime.UtcNow
};

await _oauthTokenRepository.UpsertAsync(token);
```

### 2. Dependency Injection

```csharp
public class MyService
{
    private readonly IGitHubPullRequestService _prService;
    private readonly IPullRequestTemplateService _templateService;

    public MyService(
        IGitHubPullRequestService prService,
        IPullRequestTemplateService templateService)
    {
        _prService = prService;
        _templateService = templateService;
    }
}
```

---

## Basic PR Creation

### Simple Pull Request

```csharp
var request = new CreatePullRequestRequest
{
    Title = "Add new feature",
    Body = "This PR adds a new feature to the application.",
    HeadBranch = "feature/new-feature",
    BaseBranch = "main"
};

var pr = await _prService.CreatePullRequestAsync(
    "k5tuck",           // Repository owner
    "Binelek",          // Repository name
    request,
    tenantId            // Tenant ID (for OAuth token lookup)
);

Console.WriteLine($"Pull request created: #{pr.Number}");
Console.WriteLine($"URL: {pr.HtmlUrl}");
```

### PR with Reviewers and Labels

```csharp
var request = new CreatePullRequestRequest
{
    Title = "Refactor authentication logic",
    Body = "Improves authentication flow and adds new security features.",
    HeadBranch = "refactor/auth",
    BaseBranch = "main",
    Reviewers = new List<string> { "k5tuck", "reviewer2" },
    Labels = new List<string> { "enhancement", "security", "needs-review" }
};

var pr = await _prService.CreatePullRequestAsync("k5tuck", "Binelek", request, tenantId);
```

### Draft Pull Request

```csharp
var request = new CreatePullRequestRequest
{
    Title = "WIP: Implement new API endpoints",
    Body = "Work in progress - not ready for review yet.",
    HeadBranch = "feature/new-api",
    BaseBranch = "main",
    Draft = true  // Mark as draft
};

var pr = await _prService.CreatePullRequestAsync("k5tuck", "Binelek", request, tenantId);
```

---

## Advanced PR Creation with Templates

### Ontology Refactoring PR

```csharp
// Generate PR description from template
var description = _templateService.GenerateOntologyRefactoringDescription(
    entityName: "Property",
    addedProperties: 5,
    updatedRelationships: 3,
    refactoredValidators: 2,
    files: new List<string>
    {
        "schemas/core-real-estate-ontology.yaml",
        "services/binah-ontology/Models/Property.cs",
        "services/binah-ontology/Validators/PropertyValidator.cs"
    }
);

var request = new CreatePullRequestRequest
{
    Title = "Auto-generated: Refactor Property entity",
    Body = description,
    HeadBranch = "claude/auto-refactor-property",
    BaseBranch = "main",
    Reviewers = new List<string> { "k5tuck" },
    Labels = new List<string> { "auto-generated", "ontology", "refactoring" },
    Draft = false
};

var pr = await _prService.CreatePullRequestAsync("k5tuck", "Binelek", request, tenantId);
```

### Code Generation PR

```csharp
var description = _templateService.GenerateCodeGenerationDescription(
    generatedComponent: "PropertyService CRUD Operations",
    files: new List<string>
    {
        "services/binah-ontology/Services/PropertyService.cs",
        "services/binah-ontology/Services/IPropertyService.cs",
        "services/binah-ontology/Tests/PropertyServiceTests.cs"
    },
    additionalNotes: "Generated from YAML schema v2.0 with enhanced validation rules"
);

var request = new CreatePullRequestRequest
{
    Title = "Auto-generated: PropertyService CRUD Operations",
    Body = description,
    HeadBranch = "codegen/property-service",
    BaseBranch = "main",
    Labels = new List<string> { "auto-generated", "code-generation" }
};

var pr = await _prService.CreatePullRequestAsync("k5tuck", "Binelek", request, tenantId);
```

### Bug Fix PR

```csharp
var description = _templateService.GenerateBugFixDescription(
    bugDescription: "NullReferenceException when accessing property without owner",
    fixDescription: "Added null check before accessing owner properties and improved error handling",
    files: new List<string>
    {
        "services/binah-ontology/Services/PropertyService.cs",
        "services/binah-ontology/Tests/PropertyServiceTests.cs"
    },
    issueNumber: "123"  // Links to GitHub issue #123
);

var request = new CreatePullRequestRequest
{
    Title = "Fix: NullReferenceException in PropertyService",
    Body = description,
    HeadBranch = "bugfix/property-null-check",
    BaseBranch = "main",
    Labels = new List<string> { "bug", "priority-high" }
};

var pr = await _prService.CreatePullRequestAsync("k5tuck", "Binelek", request, tenantId);
```

### Feature Addition PR

```csharp
var description = _templateService.GenerateFeatureAdditionDescription(
    featureName: "Advanced Property Search",
    featureDescription: "Adds advanced search capabilities with filters for property type, price range, location, and amenities",
    files: new List<string>
    {
        "services/binah-ontology/Controllers/PropertySearchController.cs",
        "services/binah-ontology/Services/PropertySearchService.cs",
        "services/binah-ontology/Tests/PropertySearchTests.cs"
    },
    apiEndpoints: new List<string>
    {
        "GET /api/properties/search",
        "POST /api/properties/advanced-search"
    },
    requiresDatabaseMigration: false
);

var request = new CreatePullRequestRequest
{
    Title = "Feature: Advanced Property Search",
    Body = description,
    HeadBranch = "feature/property-search",
    BaseBranch = "main",
    Reviewers = new List<string> { "k5tuck" },
    Labels = new List<string> { "feature", "enhancement" }
};

var pr = await _prService.CreatePullRequestAsync("k5tuck", "Binelek", request, tenantId);
```

---

## PR Management

### Get Pull Request Details

```csharp
var pr = await _prService.GetPullRequestAsync(
    "k5tuck",
    "Binelek",
    42,         // PR number
    tenantId
);

Console.WriteLine($"PR #{pr.Number}: {pr.Title}");
Console.WriteLine($"State: {pr.State}");
Console.WriteLine($"Mergeable: {pr.IsMergeable}");
Console.WriteLine($"Author: {pr.Author}");
Console.WriteLine($"Created: {pr.CreatedAt}");
Console.WriteLine($"Commits: {pr.Commits}");
Console.WriteLine($"Files Changed: {pr.ChangedFiles}");
Console.WriteLine($"+{pr.Additions} -{pr.Deletions}");
```

### Update Pull Request

```csharp
var updatedPr = await _prService.UpdatePullRequestAsync(
    "k5tuck",
    "Binelek",
    42,
    title: "Updated: Refactor Property entity with new validation",
    body: "This PR has been updated with additional validation rules and tests.",
    tenantId: tenantId
);
```

### Add Comment to PR

```csharp
var commentId = await _prService.AddCommentAsync(
    "k5tuck",
    "Binelek",
    42,
    "This looks good! Running CI/CD tests now.",
    tenantId
);

Console.WriteLine($"Comment added: {commentId}");
```

### Request Additional Reviewers

```csharp
var success = await _prService.RequestReviewersAsync(
    "k5tuck",
    "Binelek",
    42,
    new List<string> { "reviewer3", "reviewer4" },
    tenantId
);
```

### Add Labels

```csharp
var success = await _prService.AddLabelsAsync(
    "k5tuck",
    "Binelek",
    42,
    new List<string> { "ready-for-merge", "tested" },
    tenantId
);
```

---

## PR Status Checking

### Get Detailed PR Status

```csharp
var status = await _prService.GetPullRequestStatusAsync(
    "k5tuck",
    "Binelek",
    42,
    tenantId
);

Console.WriteLine($"PR #{status.Number} Status:");
Console.WriteLine($"State: {status.State}");
Console.WriteLine($"Mergeable: {status.IsMergeable}");
Console.WriteLine($"Mergeable State: {status.MergeableState}");
Console.WriteLine($"Checks Passed: {status.ChecksPassed}");
Console.WriteLine($"Approvals: {status.ApprovalsCount}");
Console.WriteLine($"Changes Requested: {status.ChangesRequestedCount}");
Console.WriteLine($"Can Merge: {status.CanMerge}");

if (!status.CanMerge)
{
    Console.WriteLine("Blocking Reasons:");
    foreach (var reason in status.BlockingReasons)
    {
        Console.WriteLine($"  - {reason}");
    }
}

// Check individual status checks
Console.WriteLine("\nStatus Checks:");
foreach (var check in status.StatusChecks)
{
    Console.WriteLine($"  {check.Name}: {check.Status} ({check.Conclusion})");
}

// Check reviews
Console.WriteLine("\nReviews:");
foreach (var review in status.Reviews)
{
    Console.WriteLine($"  {review.Reviewer}: {review.State}");
    if (!string.IsNullOrEmpty(review.Body))
        Console.WriteLine($"    \"{review.Body}\"");
}
```

### Wait for CI/CD Checks

```csharp
async Task<bool> WaitForChecksToPassAsync(int prNumber, TimeSpan timeout)
{
    var startTime = DateTime.UtcNow;

    while (DateTime.UtcNow - startTime < timeout)
    {
        var status = await _prService.GetPullRequestStatusAsync(
            "k5tuck",
            "Binelek",
            prNumber,
            tenantId
        );

        if (status.ChecksPassed)
        {
            Console.WriteLine("All checks passed!");
            return true;
        }

        Console.WriteLine($"Waiting for checks... ({status.StatusChecks.Count(c => c.Status == "pending")} pending)");
        await Task.Delay(TimeSpan.FromSeconds(30));
    }

    Console.WriteLine("Timeout waiting for checks");
    return false;
}
```

---

## Merging Pull Requests

### Simple Merge

```csharp
var mergeRequest = new MergePullRequestRequest
{
    CommitMessage = "Merge pull request: Refactor Property entity",
    MergeMethod = "merge"  // Options: "merge", "squash", "rebase"
};

var success = await _prService.MergePullRequestAsync(
    "k5tuck",
    "Binelek",
    42,
    mergeRequest,
    tenantId
);

if (success)
{
    Console.WriteLine("Pull request merged successfully!");
}
```

### Squash and Merge with Branch Deletion

```csharp
var mergeRequest = new MergePullRequestRequest
{
    CommitTitle = "Refactor Property entity validation",
    CommitMessage = "This commit refactors the Property entity validation logic and adds new validators.",
    MergeMethod = "squash",
    DeleteBranchAfterMerge = true
};

var success = await _prService.MergePullRequestAsync(
    "k5tuck",
    "Binelek",
    42,
    mergeRequest,
    tenantId
);
```

### Rebase and Merge

```csharp
var mergeRequest = new MergePullRequestRequest
{
    MergeMethod = "rebase",
    Sha = "abc123def456"  // Optional: ensure PR hasn't changed since last review
};

var success = await _prService.MergePullRequestAsync(
    "k5tuck",
    "Binelek",
    42,
    mergeRequest,
    tenantId
);
```

### Close PR Without Merging

```csharp
var success = await _prService.ClosePullRequestAsync(
    "k5tuck",
    "Binelek",
    42,
    tenantId
);

if (success)
{
    Console.WriteLine("Pull request closed without merging");
}
```

---

## Integration Examples

### Complete Autonomous PR Workflow

```csharp
public class AutonomousPRWorkflow
{
    private readonly IGitHubPullRequestService _prService;
    private readonly IPullRequestTemplateService _templateService;
    private readonly IGitHubBranchService _branchService;
    private readonly IGitHubCommitService _commitService;

    public async Task<int> CreateOntologyRefactoringPRAsync(
        string entityName,
        Dictionary<string, string> fileChanges,
        string tenantId)
    {
        try
        {
            // 1. Create branch
            var branchName = $"claude/auto-refactor-{entityName.ToLower()}";
            await _branchService.CreateBranchAsync(
                "k5tuck",
                "Binelek",
                branchName,
                "main",
                tenantId
            );

            // 2. Commit files
            foreach (var (filePath, content) in fileChanges)
            {
                await _commitService.CreateOrUpdateFileAsync(
                    "k5tuck",
                    "Binelek",
                    filePath,
                    content,
                    $"Refactor {entityName}: Update {Path.GetFileName(filePath)}",
                    branchName,
                    tenantId
                );
            }

            // 3. Generate PR description
            var description = _templateService.GenerateOntologyRefactoringDescription(
                entityName,
                addedProperties: 5,
                updatedRelationships: 3,
                refactoredValidators: 2,
                files: fileChanges.Keys.ToList()
            );

            // 4. Create pull request
            var request = new CreatePullRequestRequest
            {
                Title = $"Auto-generated: Refactor {entityName} entity",
                Body = description,
                HeadBranch = branchName,
                BaseBranch = "main",
                Reviewers = new List<string> { "k5tuck" },
                Labels = new List<string> { "auto-generated", "ontology" }
            };

            var pr = await _prService.CreatePullRequestAsync(
                "k5tuck",
                "Binelek",
                request,
                tenantId
            );

            // 5. Add initial comment
            await _prService.AddCommentAsync(
                "k5tuck",
                "Binelek",
                pr.Number,
                "🤖 This PR was automatically created by Binah.Regen service. Please review the changes carefully before merging.",
                tenantId
            );

            return pr.Number;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating PR: {ex.Message}");
            throw;
        }
    }
}
```

### Auto-Merge if Checks Pass

```csharp
public async Task<bool> AutoMergeIfApprovedAsync(int prNumber, string tenantId)
{
    // Get PR status
    var status = await _prService.GetPullRequestStatusAsync(
        "k5tuck",
        "Binelek",
        prNumber,
        tenantId
    );

    // Check if ready to merge
    if (status.CanMerge &&
        status.ApprovalsCount >= 1 &&
        status.ChangesRequestedCount == 0)
    {
        var mergeRequest = new MergePullRequestRequest
        {
            MergeMethod = "squash",
            DeleteBranchAfterMerge = true
        };

        var success = await _prService.MergePullRequestAsync(
            "k5tuck",
            "Binelek",
            prNumber,
            mergeRequest,
            tenantId
        );

        if (success)
        {
            await _prService.AddCommentAsync(
                "k5tuck",
                "Binelek",
                prNumber,
                "✅ Auto-merged: All checks passed and PR was approved.",
                tenantId
            );
        }

        return success;
    }

    return false;
}
```

---

## Error Handling

### Handling GitHub API Errors

```csharp
try
{
    var pr = await _prService.CreatePullRequestAsync(
        "k5tuck",
        "Binelek",
        request,
        tenantId
    );
}
catch (ArgumentException ex)
{
    // Invalid tenant ID
    Console.WriteLine($"Invalid argument: {ex.Message}");
}
catch (InvalidOperationException ex)
{
    // OAuth token not found or GitHub API error
    Console.WriteLine($"Operation failed: {ex.Message}");

    if (ex.Message.Contains("No GitHub OAuth token"))
    {
        // Redirect user to OAuth flow
        Console.WriteLine("Please authorize GitHub access first");
    }
    else if (ex.Message.Contains("already exists"))
    {
        // PR already exists for this branch
        Console.WriteLine("A pull request already exists for this branch");
    }
}
catch (Exception ex)
{
    // Other errors
    Console.WriteLine($"Unexpected error: {ex.Message}");
}
```

### Retry Logic for Transient Failures

```csharp
async Task<PullRequestResponse?> CreatePRWithRetryAsync(
    CreatePullRequestRequest request,
    string tenantId,
    int maxRetries = 3)
{
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            return await _prService.CreatePullRequestAsync(
                "k5tuck",
                "Binelek",
                request,
                tenantId
            );
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("rate limit"))
        {
            if (i == maxRetries - 1) throw;

            var delay = TimeSpan.FromSeconds(Math.Pow(2, i) * 30); // Exponential backoff
            Console.WriteLine($"Rate limited. Retrying in {delay.TotalSeconds} seconds...");
            await Task.Delay(delay);
        }
    }

    return null;
}
```

---

## Best Practices

1. **Always validate tenant OAuth tokens before creating PRs**
2. **Use descriptive branch names** (e.g., `feature/`, `bugfix/`, `claude/auto-`)
3. **Add reviewers and labels** to improve PR visibility
4. **Use draft PRs** for work-in-progress changes
5. **Check PR status** before attempting to merge
6. **Handle rate limiting** with exponential backoff
7. **Delete branches after merge** to keep repository clean
8. **Use squash merge** for cleaner commit history
9. **Add comments** to provide context for automated PRs
10. **Monitor PR status checks** before auto-merging

---

## Additional Resources

- [GitHub API Documentation](https://docs.github.com/en/rest/pulls)
- [Octokit.NET Documentation](https://octokitnet.readthedocs.io/)
- [Binah.Webhooks Service Documentation](./README.md)
- [Sprint 2 Implementation Plan](../../docs/sprint-plans/OPTION_2_GIT_INTEGRATION_SPRINT_PLAN.md)

---

**Last Updated:** 2025-11-15
**Version:** 1.0.0

# GitHub Branch and Commit Operations - Usage Guide

**Sprint 2 - Option 2: Git Integration**
**Component:** Branch and Commit Operations Services
**Last Updated:** 2025-11-15

---

## Overview

This document provides comprehensive examples for using the GitHub branch and commit services in the binah-webhooks service.

---

## Table of Contents

1. [Service Overview](#service-overview)
2. [Basic Setup](#basic-setup)
3. [Branch Operations](#branch-operations)
4. [Commit Operations](#commit-operations)
5. [Multi-File Commits](#multi-file-commits)
6. [Complete Workflow Examples](#complete-workflow-examples)
7. [Error Handling](#error-handling)
8. [Best Practices](#best-practices)

---

## Service Overview

### Services Provided

| Service | Purpose | Key Methods |
|---------|---------|-------------|
| **IGitHubBranchService** | Manage GitHub branches | Create, delete, check existence, get default branch |
| **IGitHubCommitService** | Create and manage commits | Multi-file commits, single file operations, get file content |

### Dependencies

Both services depend on:
- **IGitHubApiClient** - Initialized with tenant OAuth token
- **Octokit** - GitHub API client library

---

## Basic Setup

### 1. Initialize GitHub API Client for Tenant

Before using branch or commit services, initialize the API client with a tenant's OAuth token:

```csharp
// In a controller or service
public class MyController : ControllerBase
{
    private readonly IGitHubApiClient _apiClient;
    private readonly IGitHubBranchService _branchService;
    private readonly IGitHubCommitService _commitService;
    private readonly ITenantContext _tenantContext;

    public MyController(
        IGitHubApiClient apiClient,
        IGitHubBranchService branchService,
        IGitHubCommitService commitService,
        ITenantContext tenantContext)
    {
        _apiClient = apiClient;
        _branchService = branchService;
        _commitService = commitService;
        _tenantContext = tenantContext;
    }

    public async Task<IActionResult> PerformGitHubOperation()
    {
        // Step 1: Initialize API client for current tenant
        var tenantId = _tenantContext.TenantId;
        var initialized = await _apiClient.InitializeForTenantAsync(tenantId);

        if (!initialized)
        {
            return Unauthorized("GitHub OAuth token not found for tenant");
        }

        // Step 2: Perform operations...
        // (see examples below)

        return Ok();
    }
}
```

---

## Branch Operations

### 1. Get Branch Information

```csharp
// Get branch details
var branch = await _branchService.GetBranchAsync("k5tuck", "Binelek", "main");

Console.WriteLine($"Branch: {branch.Name}");
Console.WriteLine($"HEAD SHA: {branch.Commit.Sha}");
Console.WriteLine($"Protected: {branch.Protected}");
```

### 2. Get Default Branch

```csharp
// Get the repository's default branch (main or master)
var defaultBranch = await _branchService.GetDefaultBranchAsync("k5tuck", "Binelek");

Console.WriteLine($"Default branch: {defaultBranch}"); // "main"
```

### 3. Get Branch HEAD SHA

```csharp
// Get the SHA of the latest commit on a branch
var headSha = await _branchService.GetBranchHeadShaAsync("k5tuck", "Binelek", "main");

Console.WriteLine($"HEAD SHA: {headSha}"); // "abc123def456..."
```

### 4. Check if Branch Exists

```csharp
// Check if a branch exists before creating it
var branchName = "claude/auto-refactor-property";
var exists = await _branchService.BranchExistsAsync("k5tuck", "Binelek", branchName);

if (exists)
{
    Console.WriteLine($"Branch {branchName} already exists");
}
else
{
    Console.WriteLine($"Branch {branchName} does not exist - safe to create");
}
```

### 5. Create New Branch

```csharp
// Create a new branch from main
var owner = "k5tuck";
var repo = "Binelek";
var newBranchName = "claude/auto-refactor-property";

// Step 1: Get default branch HEAD SHA
var defaultBranch = await _branchService.GetDefaultBranchAsync(owner, repo);
var mainSha = await _branchService.GetBranchHeadShaAsync(owner, repo, defaultBranch);

// Step 2: Create new branch
var reference = await _branchService.CreateBranchAsync(owner, repo, newBranchName, mainSha);

Console.WriteLine($"Created branch: {reference.Ref}");
Console.WriteLine($"Points to SHA: {reference.Object.Sha}");
```

### 6. Delete Branch

```csharp
// Delete a branch (e.g., after PR is merged)
var branchToDelete = "claude/auto-refactor-property";

await _branchService.DeleteBranchAsync("k5tuck", "Binelek", branchToDelete);

Console.WriteLine($"Deleted branch: {branchToDelete}");
```

---

## Commit Operations

### 1. Get File Content

```csharp
// Get file from default branch
var file = await _commitService.GetFileAsync("k5tuck", "Binelek", "README.md");

Console.WriteLine($"File: {file.Path}");
Console.WriteLine($"SHA: {file.Sha}");
Console.WriteLine($"Size: {file.Size} bytes");
Console.WriteLine($"Content: {file.Content}");

// Get file from specific branch
var fileFromBranch = await _commitService.GetFileAsync(
    "k5tuck",
    "Binelek",
    "schemas/property.yaml",
    branchName: "feature/new-schema"
);
```

### 2. Create Single File

```csharp
// Create a new file
var result = await _commitService.CreateFileAsync(
    owner: "k5tuck",
    repo: "Binelek",
    path: "schemas/new-entity.yaml",
    content: "# New Entity Schema\nentities:\n  NewEntity:\n    description: Test",
    message: "feat(schema): Add new entity schema",
    branchName: "claude/new-entity"
);

Console.WriteLine($"Created file commit: {result.Commit.Sha}");
```

### 3. Update Single File

```csharp
// Update an existing file
var existingFile = await _commitService.GetFileAsync("k5tuck", "Binelek", "README.md");

var updatedContent = existingFile.Content + "\n\n## New Section\nAdded by Claude";

var result = await _commitService.UpdateFileAsync(
    owner: "k5tuck",
    repo: "Binelek",
    path: "README.md",
    content: updatedContent,
    message: "docs: Add new section to README",
    branchName: "claude/update-docs",
    sha: existingFile.Sha // Required for conflict detection
);

Console.WriteLine($"Updated file commit: {result.Commit.Sha}");
```

### 4. Delete Single File

```csharp
// Delete a file
var fileToDelete = await _commitService.GetFileAsync("k5tuck", "Binelek", "old-file.txt");

await _commitService.DeleteFileAsync(
    owner: "k5tuck",
    repo: "Binelek",
    path: "old-file.txt",
    message: "chore: Remove deprecated file",
    branchName: "claude/cleanup",
    sha: fileToDelete.Sha
);

Console.WriteLine("File deleted successfully");
```

### 5. Get Commit Details

```csharp
// Get commit information
var commit = await _commitService.GetCommitAsync("k5tuck", "Binelek", "abc123def456");

Console.WriteLine($"Author: {commit.Author.Name}");
Console.WriteLine($"Message: {commit.Message}");
Console.WriteLine($"Tree SHA: {commit.Tree.Sha}");
```

---

## Multi-File Commits

### Example 1: Create Commit with Multiple New Files

```csharp
using Binah.Webhooks.Models.DTOs.GitHub;

// Define file changes
var files = new List<GitHubFileChange>
{
    new GitHubFileChange
    {
        Path = "schemas/property.yaml",
        Content = "# Property Schema\nentities:\n  Property:\n    description: Real estate property",
        Mode = GitHubFileChangeMode.Add
    },
    new GitHubFileChange
    {
        Path = "schemas/owner.yaml",
        Content = "# Owner Schema\nentities:\n  Owner:\n    description: Property owner",
        Mode = GitHubFileChangeMode.Add
    },
    new GitHubFileChange
    {
        Path = "README.md",
        Content = "# Updated README\n\nAdded new schemas",
        Mode = GitHubFileChangeMode.Update
    }
};

// Create atomic commit with all changes
var commitSha = await _commitService.CreateCommitAsync(
    owner: "k5tuck",
    repo: "Binelek",
    branchName: "claude/add-schemas",
    message: "feat(schemas): Add property and owner schemas\n\nAdds:\n- property.yaml\n- owner.yaml\n\nUpdates:\n- README.md",
    files: files
);

Console.WriteLine($"Created multi-file commit: {commitSha}");
```

### Example 2: Mixed Operations (Add, Update, Delete)

```csharp
var files = new List<GitHubFileChange>
{
    // Add new file
    new GitHubFileChange
    {
        Path = "schemas/new-entity.yaml",
        Content = "# New entity",
        Mode = GitHubFileChangeMode.Add
    },

    // Update existing file
    new GitHubFileChange
    {
        Path = "schemas/property.yaml",
        Content = "# Updated property schema",
        Mode = GitHubFileChangeMode.Update
    },

    // Delete old file
    new GitHubFileChange
    {
        Path = "schemas/deprecated.yaml",
        Mode = GitHubFileChangeMode.Delete
    }
};

var commitSha = await _commitService.CreateCommitAsync(
    owner: "k5tuck",
    repo: "Binelek",
    branchName: "claude/refactor-schemas",
    message: "refactor(schemas): Refactor entity schemas",
    files: files
);
```

---

## Complete Workflow Examples

### Workflow 1: Create Feature Branch with Changes

```csharp
public async Task<string> CreateFeatureBranchWithChanges()
{
    var owner = "k5tuck";
    var repo = "Binelek";
    var branchName = "claude/auto-refactor-property";

    // Step 1: Get main branch HEAD SHA
    var mainBranch = await _branchService.GetDefaultBranchAsync(owner, repo);
    var mainSha = await _branchService.GetBranchHeadShaAsync(owner, repo, mainBranch);

    // Step 2: Create new branch
    await _branchService.CreateBranchAsync(owner, repo, branchName, mainSha);

    // Step 3: Create commit with changes
    var files = new List<GitHubFileChange>
    {
        new()
        {
            Path = "schemas/property.yaml",
            Content = GenerateRefactoredSchema(),
            Mode = GitHubFileChangeMode.Update
        }
    };

    var commitSha = await _commitService.CreateCommitAsync(
        owner,
        repo,
        branchName,
        "refactor(ontology): Auto-generated property schema refactoring",
        files
    );

    Console.WriteLine($"Created branch {branchName} with commit {commitSha}");

    return branchName;
}

private string GenerateRefactoredSchema()
{
    // Your schema generation logic here
    return "# Refactored Schema\n...";
}
```

### Workflow 2: Autonomous Documentation Update

```csharp
public async Task UpdateDocumentationAutonomously()
{
    var owner = "k5tuck";
    var repo = "Binelek";
    var branchName = "claude/sync-main-documentation";

    // Step 1: Check if branch exists
    var exists = await _branchService.BranchExistsAsync(owner, repo, branchName);

    if (!exists)
    {
        // Create branch from main
        var mainBranch = await _branchService.GetDefaultBranchAsync(owner, repo);
        var mainSha = await _branchService.GetBranchHeadShaAsync(owner, repo, mainBranch);
        await _branchService.CreateBranchAsync(owner, repo, branchName, mainSha);
    }

    // Step 2: Prepare documentation updates
    var files = new List<GitHubFileChange>
    {
        new()
        {
            Path = "docs/ARCHITECTURE.md",
            Content = await GenerateArchitectureDoc(),
            Mode = GitHubFileChangeMode.Update
        },
        new()
        {
            Path = "docs/API_REFERENCE.md",
            Content = await GenerateApiReference(),
            Mode = GitHubFileChangeMode.Update
        },
        new()
        {
            Path = "CHANGELOG.md",
            Content = await GenerateChangelog(),
            Mode = GitHubFileChangeMode.Update
        }
    };

    // Step 3: Create commit
    var commitSha = await _commitService.CreateCommitAsync(
        owner,
        repo,
        branchName,
        "docs: Sync documentation with latest changes\n\nAuto-generated by Claude Agent",
        files
    );

    Console.WriteLine($"Updated documentation in commit {commitSha}");
}

private async Task<string> GenerateArchitectureDoc()
{
    // Your doc generation logic
    return "# Architecture\n...";
}

private async Task<string> GenerateApiReference()
{
    return "# API Reference\n...";
}

private async Task<string> GenerateChangelog()
{
    return "# Changelog\n...";
}
```

### Workflow 3: Code Generation and Commit

```csharp
public async Task<string> GenerateAndCommitCode(string entityName)
{
    var owner = "k5tuck";
    var repo = "Binelek";
    var branchName = $"claude/codegen-{entityName.ToLower()}";

    // Step 1: Create branch
    var mainBranch = await _branchService.GetDefaultBranchAsync(owner, repo);
    var mainSha = await _branchService.GetBranchHeadShaAsync(owner, repo, mainBranch);
    await _branchService.CreateBranchAsync(owner, repo, branchName, mainSha);

    // Step 2: Generate code files
    var files = new List<GitHubFileChange>
    {
        new()
        {
            Path = $"services/binah-ontology/Models/{entityName}.cs",
            Content = GenerateEntityModel(entityName),
            Mode = GitHubFileChangeMode.Add
        },
        new()
        {
            Path = $"services/binah-ontology/Repositories/{entityName}Repository.cs",
            Content = GenerateRepository(entityName),
            Mode = GitHubFileChangeMode.Add
        },
        new()
        {
            Path = $"services/binah-ontology/Services/{entityName}Service.cs",
            Content = GenerateService(entityName),
            Mode = GitHubFileChangeMode.Add
        }
    };

    // Step 3: Commit generated code
    var commitSha = await _commitService.CreateCommitAsync(
        owner,
        repo,
        branchName,
        $"feat({entityName.ToLower()}): Auto-generate {entityName} entity code\n\nGenerated by Binah.Regen service",
        files
    );

    return branchName;
}

private string GenerateEntityModel(string entityName)
{
    return $@"namespace Binah.Ontology.Models;

public class {entityName}
{{
    public Guid Id {{ get; set; }}
    public string Name {{ get; set; }} = string.Empty;
}}";
}

private string GenerateRepository(string entityName)
{
    return $"// {entityName}Repository implementation";
}

private string GenerateService(string entityName)
{
    return $"// {entityName}Service implementation";
}
```

---

## Error Handling

### Handle Common Errors

```csharp
try
{
    var branch = await _branchService.GetBranchAsync("k5tuck", "Binelek", "feature/test");
}
catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
{
    _logger.LogWarning("Branch does not exist: {Message}", ex.Message);
    // Handle branch not found
}
catch (InvalidOperationException ex) when (ex.Message.Contains("not initialized"))
{
    _logger.LogError("GitHub API client not initialized: {Message}", ex.Message);
    // Re-initialize client
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error getting branch");
    throw;
}
```

### Handle Branch Already Exists

```csharp
var branchName = "claude/feature";

try
{
    // Check first
    if (await _branchService.BranchExistsAsync("k5tuck", "Binelek", branchName))
    {
        _logger.LogWarning("Branch {BranchName} already exists - using existing branch", branchName);
        // Use existing branch or delete and recreate
    }
    else
    {
        await _branchService.CreateBranchAsync("k5tuck", "Binelek", branchName, mainSha);
    }
}
catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
{
    _logger.LogWarning("Caught race condition - branch created by another process");
    // Handle race condition
}
```

### Handle File Conflicts

```csharp
try
{
    var existingFile = await _commitService.GetFileAsync("k5tuck", "Binelek", "README.md");

    await _commitService.UpdateFileAsync(
        "k5tuck",
        "Binelek",
        "README.md",
        updatedContent,
        "Update README",
        "main",
        existingFile.Sha // Important: use current SHA
    );
}
catch (InvalidOperationException ex) when (ex.Message.Contains("conflict"))
{
    _logger.LogWarning("File has been modified - SHA mismatch");
    // Re-fetch file and retry
    var latestFile = await _commitService.GetFileAsync("k5tuck", "Binelek", "README.md");
    // Merge changes or abort
}
```

---

## Best Practices

### 1. Always Initialize API Client First

```csharp
// ❌ WRONG: Calling service without initialization
var branch = await _branchService.GetBranchAsync(...); // Will throw InvalidOperationException

// ✅ CORRECT: Initialize first
await _apiClient.InitializeForTenantAsync(tenantId);
var branch = await _branchService.GetBranchAsync(...);
```

### 2. Check Branch Existence Before Creating

```csharp
// ✅ BEST PRACTICE: Check before creating
if (!await _branchService.BranchExistsAsync(owner, repo, branchName))
{
    await _branchService.CreateBranchAsync(owner, repo, branchName, fromSha);
}
```

### 3. Use Atomic Multi-File Commits

```csharp
// ❌ WRONG: Multiple separate commits (creates multiple commits)
await _commitService.CreateFileAsync(..., "file1.txt", ...);
await _commitService.CreateFileAsync(..., "file2.txt", ...);
await _commitService.CreateFileAsync(..., "file3.txt", ...);

// ✅ CORRECT: Single atomic commit with all files
var files = new List<GitHubFileChange>
{
    new() { Path = "file1.txt", Content = "...", Mode = GitHubFileChangeMode.Add },
    new() { Path = "file2.txt", Content = "...", Mode = GitHubFileChangeMode.Add },
    new() { Path = "file3.txt", Content = "...", Mode = GitHubFileChangeMode.Add }
};

await _commitService.CreateCommitAsync(..., files);
```

### 4. Follow Conventional Commit Messages

```csharp
// ✅ GOOD: Clear, conventional commit message
var message = @"feat(schemas): Add property and owner entities

- Add property.yaml with Property entity definition
- Add owner.yaml with Owner entity definition
- Update README.md with new entities

Generated by Binah.Regen service
";

await _commitService.CreateCommitAsync(..., message, files);
```

### 5. Use Descriptive Branch Names

```csharp
// ✅ GOOD: Clear branch naming convention
var branchName = $"claude/{workflow}-{entityName}-{timestamp}";
// Example: "claude/codegen-property-20251115"

// ❌ BAD: Vague branch name
var branchName = "temp";
```

### 6. Clean Up Branches After PR Merge

```csharp
// After PR is merged
await _branchService.DeleteBranchAsync(owner, repo, branchName);
```

### 7. Use File SHAs for Conflict Detection

```csharp
// ✅ GOOD: Get current SHA before update
var file = await _commitService.GetFileAsync(owner, repo, path);
await _commitService.UpdateFileAsync(..., sha: file.Sha);

// ❌ BAD: Hardcoded or missing SHA
await _commitService.UpdateFileAsync(..., sha: "abc123"); // May cause conflicts
```

---

## Integration with Pull Request Service

After creating commits, you can create a pull request using `IGitHubPullRequestService` (Sprint 2 Agent 3):

```csharp
// Step 1: Create branch and commit
var branchName = await CreateFeatureBranchWithChanges();

// Step 2: Create pull request
var pr = await _pullRequestService.CreatePullRequestAsync(
    owner: "k5tuck",
    repo: "Binelek",
    baseBranch: "main",
    headBranch: branchName,
    title: "Auto-generated: Refactor property schema",
    body: "AI-generated schema refactoring by Claude Agent"
);

Console.WriteLine($"Created PR #{pr.Number}: {pr.Url}");
```

---

## Summary

This guide covered:
- ✅ Branch operations (create, delete, check existence)
- ✅ Commit operations (single file and multi-file)
- ✅ Complete workflows (feature branches, documentation updates, code generation)
- ✅ Error handling patterns
- ✅ Best practices

For more information, see:
- [Sprint Plan](/home/user/Binelek/docs/sprint-plans/OPTION_2_GIT_INTEGRATION_SPRINT_PLAN.md)
- [GitHub API Documentation](https://docs.github.com/en/rest)
- [Octokit.NET Documentation](https://octokitnet.readthedocs.io/)

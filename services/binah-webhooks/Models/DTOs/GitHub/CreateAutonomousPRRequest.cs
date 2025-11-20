using Binah.Webhooks.Models.DTOs.GitHub;

namespace Binah.Webhooks.Models.DTOs.GitHub;

/// <summary>
/// Request to create an autonomous pull request with complete workflow orchestration
/// </summary>
public class CreateAutonomousPRRequest
{
    /// <summary>
    /// Tenant identifier
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Repository owner (e.g., "k5tuck")
    /// </summary>
    public string RepositoryOwner { get; set; } = string.Empty;

    /// <summary>
    /// Repository name (e.g., "Binelek")
    /// </summary>
    public string RepositoryName { get; set; } = string.Empty;

    /// <summary>
    /// Base branch to create PR against (e.g., "main")
    /// </summary>
    public string BaseBranch { get; set; } = "main";

    /// <summary>
    /// Prefix for the auto-generated branch name (e.g., "claude/auto-refactor")
    /// Will be combined with timestamp and random suffix
    /// </summary>
    public string BranchPrefix { get; set; } = "claude/autonomous";

    /// <summary>
    /// Pull request title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Workflow type that triggered this PR
    /// </summary>
    public WorkflowType WorkflowType { get; set; } = WorkflowType.OntologyRefactoring;

    /// <summary>
    /// List of file changes to commit
    /// </summary>
    public List<GitHubFileChange> Files { get; set; } = new();

    /// <summary>
    /// Template data for generating PR description
    /// Key-value pairs used by IPullRequestTemplateService
    /// </summary>
    public Dictionary<string, string> TemplateData { get; set; } = new();

    /// <summary>
    /// Whether to automatically merge the PR after CI checks pass
    /// </summary>
    public bool AutoMerge { get; set; } = false;

    /// <summary>
    /// List of GitHub usernames to request reviews from
    /// </summary>
    public List<string> Reviewers { get; set; } = new();

    /// <summary>
    /// List of labels to add to the pull request
    /// </summary>
    public List<string> Labels { get; set; } = new();

    /// <summary>
    /// Commit message for the changes
    /// </summary>
    public string CommitMessage { get; set; } = string.Empty;

    /// <summary>
    /// Create as a draft pull request (not ready for review)
    /// </summary>
    public bool Draft { get; set; } = false;
}

/// <summary>
/// Type of workflow that triggered the autonomous PR
/// </summary>
public enum WorkflowType
{
    /// <summary>
    /// Ontology refactoring workflow (schema changes)
    /// </summary>
    OntologyRefactoring,

    /// <summary>
    /// Code generation workflow (generated code from templates)
    /// </summary>
    CodeGeneration,

    /// <summary>
    /// Bug fix workflow
    /// </summary>
    BugFix,

    /// <summary>
    /// Feature addition workflow
    /// </summary>
    FeatureAddition,

    /// <summary>
    /// General refactoring workflow
    /// </summary>
    Refactoring,

    /// <summary>
    /// Documentation update workflow
    /// </summary>
    Documentation
}

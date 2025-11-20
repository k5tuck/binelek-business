namespace Binah.Webhooks.Models.DTOs.GitHub;

/// <summary>
/// Request to merge a pull request
/// </summary>
public class MergePullRequestRequest
{
    /// <summary>
    /// Commit message for the merge commit
    /// If not provided, GitHub will generate one
    /// </summary>
    public string? CommitMessage { get; set; }

    /// <summary>
    /// Additional commit message details
    /// </summary>
    public string? CommitTitle { get; set; }

    /// <summary>
    /// Merge method to use
    /// - "merge": Create a merge commit
    /// - "squash": Squash all commits into one
    /// - "rebase": Rebase commits onto base branch
    /// </summary>
    public string MergeMethod { get; set; } = "merge";

    /// <summary>
    /// SHA that pull request head must match to allow merge
    /// Used to ensure the PR hasn't changed since last review
    /// </summary>
    public string? Sha { get; set; }

    /// <summary>
    /// Delete the head branch after merge
    /// </summary>
    public bool DeleteBranchAfterMerge { get; set; } = false;
}

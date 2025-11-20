using Binah.Webhooks.Models.DTOs.GitHub;
using Binah.Webhooks.Repositories.Interfaces;
using Binah.Webhooks.Services.Interfaces;
using Octokit;
using Serilog;

namespace Binah.Webhooks.Services.Implementations;

/// <summary>
/// Service for GitHub pull request operations using Octokit.NET
/// </summary>
public class GitHubPullRequestService : IGitHubPullRequestService
{
    private readonly IGitHubOAuthTokenRepository _tokenRepository;
    private readonly ILogger _logger;

    public GitHubPullRequestService(
        IGitHubOAuthTokenRepository tokenRepository,
        ILogger logger)
    {
        _tokenRepository = tokenRepository;
        _logger = logger;
    }

    /// <summary>
    /// Create a GitHub client authenticated with the tenant's OAuth token
    /// </summary>
    private async Task<GitHubClient> CreateAuthenticatedClientAsync(string tenantId)
    {
        if (!Guid.TryParse(tenantId, out var tenantGuid))
        {
            throw new ArgumentException($"Invalid tenant ID: {tenantId}", nameof(tenantId));
        }

        var token = await _tokenRepository.GetByTenantAsync(tenantGuid);
        if (token == null)
        {
            throw new InvalidOperationException($"No GitHub OAuth token found for tenant {tenantId}");
        }

        var client = new GitHubClient(new ProductHeaderValue("Binah-Webhooks"))
        {
            Credentials = new Credentials(token.AccessToken)
        };

        return client;
    }

    /// <inheritdoc/>
    public async Task<PullRequestResponse> CreatePullRequestAsync(
        string owner,
        string repo,
        CreatePullRequestRequest request,
        string tenantId)
    {
        try
        {
            _logger.Information(
                "Creating pull request in {Owner}/{Repo}: {Title} ({HeadBranch} -> {BaseBranch})",
                owner, repo, request.Title, request.HeadBranch, request.BaseBranch);

            var client = await CreateAuthenticatedClientAsync(tenantId);

            // Create the pull request
            var newPr = new NewPullRequest(request.Title, request.HeadBranch, request.BaseBranch)
            {
                Body = request.Body,
                Draft = request.Draft,
                MaintainerCanModify = request.MaintainerCanModify
            };

            var pr = await client.PullRequest.Create(owner, repo, newPr);

            _logger.Information("Pull request created: #{Number} - {Url}", pr.Number, pr.HtmlUrl);

            // Add reviewers if specified
            if (request.Reviewers.Any())
            {
                await RequestReviewersAsync(owner, repo, pr.Number, request.Reviewers, tenantId);
            }

            // Add labels if specified
            if (request.Labels.Any())
            {
                await AddLabelsAsync(owner, repo, pr.Number, request.Labels, tenantId);
            }

            return MapPullRequestToResponse(pr);
        }
        catch (ApiException ex)
        {
            _logger.Error(ex, "GitHub API error creating pull request: {Message}", ex.Message);
            throw new InvalidOperationException($"Failed to create pull request: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error creating pull request");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<PullRequestResponse> GetPullRequestAsync(
        string owner,
        string repo,
        int prNumber,
        string tenantId)
    {
        try
        {
            _logger.Information("Getting pull request {Owner}/{Repo}#{Number}", owner, repo, prNumber);

            var client = await CreateAuthenticatedClientAsync(tenantId);
            var pr = await client.PullRequest.Get(owner, repo, prNumber);

            return MapPullRequestToResponse(pr);
        }
        catch (ApiException ex)
        {
            _logger.Error(ex, "GitHub API error getting pull request: {Message}", ex.Message);
            throw new InvalidOperationException($"Failed to get pull request: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting pull request");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<PullRequestResponse> UpdatePullRequestAsync(
        string owner,
        string repo,
        int prNumber,
        string? title = null,
        string? body = null,
        string? tenantId = null)
    {
        try
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                throw new ArgumentException("Tenant ID is required", nameof(tenantId));
            }

            _logger.Information("Updating pull request {Owner}/{Repo}#{Number}", owner, repo, prNumber);

            var client = await CreateAuthenticatedClientAsync(tenantId);

            var update = new PullRequestUpdate();
            if (!string.IsNullOrEmpty(title))
                update.Title = title;
            if (!string.IsNullOrEmpty(body))
                update.Body = body;

            var pr = await client.PullRequest.Update(owner, repo, prNumber, update);

            _logger.Information("Pull request updated: #{Number}", pr.Number);

            return MapPullRequestToResponse(pr);
        }
        catch (ApiException ex)
        {
            _logger.Error(ex, "GitHub API error updating pull request: {Message}", ex.Message);
            throw new InvalidOperationException($"Failed to update pull request: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error updating pull request");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> MergePullRequestAsync(
        string owner,
        string repo,
        int prNumber,
        MergePullRequestRequest request,
        string tenantId)
    {
        try
        {
            _logger.Information(
                "Merging pull request {Owner}/{Repo}#{Number} using {MergeMethod}",
                owner, repo, prNumber, request.MergeMethod);

            var client = await CreateAuthenticatedClientAsync(tenantId);

            // Map merge method string to enum
            var mergeMethod = request.MergeMethod.ToLowerInvariant() switch
            {
                "merge" => PullRequestMergeMethod.Merge,
                "squash" => PullRequestMergeMethod.Squash,
                "rebase" => PullRequestMergeMethod.Rebase,
                _ => PullRequestMergeMethod.Merge
            };

            var mergePr = new MergePullRequest
            {
                CommitMessage = request.CommitMessage,
                CommitTitle = request.CommitTitle,
                MergeMethod = mergeMethod,
                Sha = request.Sha
            };

            var mergeResult = await client.PullRequest.Merge(owner, repo, prNumber, mergePr);

            if (mergeResult.Merged)
            {
                _logger.Information("Pull request merged successfully: #{Number}", prNumber);

                // Delete branch if requested
                if (request.DeleteBranchAfterMerge)
                {
                    try
                    {
                        var pr = await client.PullRequest.Get(owner, repo, prNumber);
                        var headRef = pr.Head.Ref;
                        await client.Git.Reference.Delete(owner, repo, $"heads/{headRef}");
                        _logger.Information("Deleted branch {Branch} after merge", headRef);
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning(ex, "Failed to delete branch after merge");
                    }
                }

                return true;
            }

            _logger.Warning("Pull request merge failed: {Message}", mergeResult.Message);
            return false;
        }
        catch (ApiException ex)
        {
            _logger.Error(ex, "GitHub API error merging pull request: {Message}", ex.Message);
            throw new InvalidOperationException($"Failed to merge pull request: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error merging pull request");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ClosePullRequestAsync(
        string owner,
        string repo,
        int prNumber,
        string tenantId)
    {
        try
        {
            _logger.Information("Closing pull request {Owner}/{Repo}#{Number}", owner, repo, prNumber);

            var client = await CreateAuthenticatedClientAsync(tenantId);

            var update = new PullRequestUpdate
            {
                State = ItemState.Closed
            };

            var pr = await client.PullRequest.Update(owner, repo, prNumber, update);

            _logger.Information("Pull request closed: #{Number}", pr.Number);

            return pr.State == ItemState.Closed;
        }
        catch (ApiException ex)
        {
            _logger.Error(ex, "GitHub API error closing pull request: {Message}", ex.Message);
            throw new InvalidOperationException($"Failed to close pull request: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error closing pull request");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<long> AddCommentAsync(
        string owner,
        string repo,
        int prNumber,
        string comment,
        string tenantId)
    {
        try
        {
            _logger.Information("Adding comment to pull request {Owner}/{Repo}#{Number}", owner, repo, prNumber);

            var client = await CreateAuthenticatedClientAsync(tenantId);

            var issueComment = await client.Issue.Comment.Create(owner, repo, prNumber, comment);

            _logger.Information("Comment added: {CommentId}", issueComment.Id);

            return issueComment.Id;
        }
        catch (ApiException ex)
        {
            _logger.Error(ex, "GitHub API error adding comment: {Message}", ex.Message);
            throw new InvalidOperationException($"Failed to add comment: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error adding comment");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> RequestReviewersAsync(
        string owner,
        string repo,
        int prNumber,
        List<string> reviewers,
        string tenantId)
    {
        try
        {
            _logger.Information(
                "Requesting reviewers for pull request {Owner}/{Repo}#{Number}: {Reviewers}",
                owner, repo, prNumber, string.Join(", ", reviewers));

            var client = await CreateAuthenticatedClientAsync(tenantId);

            var reviewRequest = new PullRequestReviewRequest(reviewers, new List<string>());
            var result = await client.PullRequest.ReviewRequest.Create(owner, repo, prNumber, reviewRequest);

            _logger.Information("Reviewers requested successfully");

            return true;
        }
        catch (ApiException ex)
        {
            _logger.Error(ex, "GitHub API error requesting reviewers: {Message}", ex.Message);
            // Don't throw - invalid reviewers shouldn't fail the whole operation
            return false;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error requesting reviewers");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> AddLabelsAsync(
        string owner,
        string repo,
        int prNumber,
        List<string> labels,
        string tenantId)
    {
        try
        {
            _logger.Information(
                "Adding labels to pull request {Owner}/{Repo}#{Number}: {Labels}",
                owner, repo, prNumber, string.Join(", ", labels));

            var client = await CreateAuthenticatedClientAsync(tenantId);

            var result = await client.Issue.Labels.AddToIssue(owner, repo, prNumber, labels.ToArray());

            _logger.Information("Labels added successfully");

            return true;
        }
        catch (ApiException ex)
        {
            _logger.Error(ex, "GitHub API error adding labels: {Message}", ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error adding labels");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<PullRequestStatusResponse> GetPullRequestStatusAsync(
        string owner,
        string repo,
        int prNumber,
        string tenantId)
    {
        try
        {
            _logger.Information("Getting status for pull request {Owner}/{Repo}#{Number}", owner, repo, prNumber);

            var client = await CreateAuthenticatedClientAsync(tenantId);

            // Get PR details
            var pr = await client.PullRequest.Get(owner, repo, prNumber);

            // Get reviews
            var reviews = await client.PullRequest.Review.GetAll(owner, repo, prNumber);

            // Get commits to check status checks
            var commits = await client.PullRequest.Commits(owner, repo, prNumber);
            var latestCommit = commits.LastOrDefault();

            // Get status checks for latest commit
            var statusChecks = new List<StatusCheck>();
            var checksPassed = true;

            if (latestCommit != null)
            {
                try
                {
                    var combinedStatus = await client.Repository.Status.GetCombined(owner, repo, latestCommit.Sha);

                    foreach (var status in combinedStatus.Statuses)
                    {
                        statusChecks.Add(new StatusCheck
                        {
                            Name = status.Context,
                            Status = status.State.StringValue,
                            Conclusion = status.State.StringValue,
                            DetailsUrl = status.TargetUrl
                        });

                        if (status.State.Value != CommitState.Success)
                        {
                            checksPassed = false;
                        }
                    }

                    // Also get check runs (GitHub Actions)
                    var checkRuns = await client.Check.Run.GetAllForReference(owner, repo, latestCommit.Sha);
                    foreach (var checkRun in checkRuns.CheckRuns)
                    {
                        statusChecks.Add(new StatusCheck
                        {
                            Name = checkRun.Name,
                            Status = checkRun.Status.StringValue,
                            Conclusion = checkRun.Conclusion?.StringValue,
                            DetailsUrl = checkRun.HtmlUrl
                        });

                        if (checkRun.Conclusion?.Value != CheckConclusion.Success &&
                            checkRun.Conclusion?.Value != CheckConclusion.Neutral &&
                            checkRun.Status.Value != CheckStatus.Completed)
                        {
                            checksPassed = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Failed to get status checks");
                }
            }

            // Count approvals and changes requested
            var approvalsCount = reviews.Count(r => r.State.Value == PullRequestReviewState.Approved);
            var changesRequestedCount = reviews.Count(r => r.State.Value == PullRequestReviewState.ChangesRequested);

            // Determine if can merge
            var canMerge = pr.Mergeable == true && checksPassed && changesRequestedCount == 0;
            var blockingReasons = new List<string>();

            if (pr.Mergeable == false)
                blockingReasons.Add("Pull request has merge conflicts");
            if (!checksPassed)
                blockingReasons.Add("Status checks have not passed");
            if (changesRequestedCount > 0)
                blockingReasons.Add($"{changesRequestedCount} reviewer(s) requested changes");

            var response = new PullRequestStatusResponse
            {
                Number = pr.Number,
                State = pr.State.StringValue,
                IsMergeable = pr.Mergeable,
                MergeableState = pr.MergeableState?.StringValue ?? "unknown",
                ChecksPassed = checksPassed,
                StatusChecks = statusChecks,
                ApprovalsCount = approvalsCount,
                ChangesRequestedCount = changesRequestedCount,
                Reviews = reviews.Select(r => new Review
                {
                    Reviewer = r.User.Login,
                    State = r.State.StringValue,
                    Body = r.Body,
                    SubmittedAt = r.SubmittedAt
                }).ToList(),
                CanMerge = canMerge,
                BlockingReasons = blockingReasons
            };

            return response;
        }
        catch (ApiException ex)
        {
            _logger.Error(ex, "GitHub API error getting pull request status: {Message}", ex.Message);
            throw new InvalidOperationException($"Failed to get pull request status: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting pull request status");
            throw;
        }
    }

    /// <summary>
    /// Map Octokit PullRequest to our DTO
    /// </summary>
    private PullRequestResponse MapPullRequestToResponse(PullRequest pr)
    {
        return new PullRequestResponse
        {
            Number = pr.Number,
            Title = pr.Title,
            Body = pr.Body ?? string.Empty,
            State = pr.State.StringValue,
            HeadBranch = pr.Head.Ref,
            BaseBranch = pr.Base.Ref,
            Url = pr.Url,
            HtmlUrl = pr.HtmlUrl,
            IsDraft = pr.Draft,
            IsMerged = pr.Merged,
            IsMergeable = pr.Mergeable,
            Author = pr.User.Login,
            CreatedAt = pr.CreatedAt.DateTime,
            UpdatedAt = pr.UpdatedAt?.DateTime,
            MergedAt = pr.MergedAt?.DateTime,
            ClosedAt = pr.ClosedAt?.DateTime,
            Commits = pr.Commits,
            ChangedFiles = pr.ChangedFiles,
            Additions = pr.Additions,
            Deletions = pr.Deletions,
            Labels = pr.Labels.Select(l => l.Name).ToList(),
            RequestedReviewers = pr.RequestedReviewers.Select(r => r.Login).ToList()
        };
    }
}

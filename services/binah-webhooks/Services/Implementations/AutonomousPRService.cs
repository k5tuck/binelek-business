using Binah.Webhooks.Models.DTOs.GitHub;
using Binah.Webhooks.Models.Domain;
using Binah.Webhooks.Models.Events;
using Binah.Webhooks.Models.DTOs.Notifications;
using Binah.Webhooks.Services.Interfaces;
using Binah.Webhooks.Repositories.Interfaces;
using Binah.Infrastructure.Kafka;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Binah.Webhooks.Services.Implementations;

/// <summary>
/// Service for orchestrating autonomous pull request creation
/// Handles complete workflow: branch → commit → PR → tracking → events
/// </summary>
public class AutonomousPRService : IAutonomousPRService
{
    private readonly IGitHubBranchService _branchService;
    private readonly IGitHubCommitService _commitService;
    private readonly IGitHubPullRequestService _pullRequestService;
    private readonly IPullRequestTemplateService _templateService;
    private readonly IAutonomousPullRequestRepository _repository;
    private readonly INotificationService _notificationService;
    private readonly KafkaProducer _kafkaProducer;
    private readonly ILogger<AutonomousPRService> _logger;

    // Kafka topic names
    private const string TopicPRCreated = "autonomous.pr.created.v1";
    private const string TopicPRMerged = "autonomous.pr.merged.v1";
    private const string TopicPRFailed = "autonomous.pr.failed.v1";

    public AutonomousPRService(
        IGitHubBranchService branchService,
        IGitHubCommitService commitService,
        IGitHubPullRequestService pullRequestService,
        IPullRequestTemplateService templateService,
        IAutonomousPullRequestRepository repository,
        INotificationService notificationService,
        KafkaProducer kafkaProducer,
        ILogger<AutonomousPRService> logger)
    {
        _branchService = branchService ?? throw new ArgumentNullException(nameof(branchService));
        _commitService = commitService ?? throw new ArgumentNullException(nameof(commitService));
        _pullRequestService = pullRequestService ?? throw new ArgumentNullException(nameof(pullRequestService));
        _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _kafkaProducer = kafkaProducer ?? throw new ArgumentNullException(nameof(kafkaProducer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Create an autonomous pull request with complete workflow orchestration
    /// </summary>
    public async Task<CreateAutonomousPRResponse> CreateAutonomousPRAsync(CreateAutonomousPRRequest request)
    {
        var correlationId = Guid.NewGuid().ToString();
        string? branchName = null;
        string? commitSha = null;

        _logger.LogInformation(
            "Starting autonomous PR creation. Tenant: {TenantId}, Repository: {Owner}/{Repo}, WorkflowType: {WorkflowType}, CorrelationId: {CorrelationId}",
            request.TenantId, request.RepositoryOwner, request.RepositoryName, request.WorkflowType, correlationId);

        try
        {
            // Step 1: Generate unique branch name
            branchName = GenerateUniqueBranchName(request.BranchPrefix);
            _logger.LogInformation("Generated branch name: {BranchName}", branchName);

            // Step 2: Check if branch already exists (cleanup if needed)
            var branchExists = await _branchService.BranchExistsAsync(
                request.RepositoryOwner,
                request.RepositoryName,
                branchName);

            if (branchExists)
            {
                _logger.LogWarning("Branch {BranchName} already exists. Deleting and recreating.", branchName);
                await _branchService.DeleteBranchAsync(
                    request.RepositoryOwner,
                    request.RepositoryName,
                    branchName);
            }

            // Step 3: Get default branch SHA
            var baseBranchSha = await _branchService.GetBranchHeadShaAsync(
                request.RepositoryOwner,
                request.RepositoryName,
                request.BaseBranch);

            _logger.LogInformation("Base branch {BaseBranch} SHA: {Sha}", request.BaseBranch, baseBranchSha);

            // Step 4: Create new branch
            await _branchService.CreateBranchAsync(
                request.RepositoryOwner,
                request.RepositoryName,
                branchName,
                baseBranchSha);

            _logger.LogInformation("Created branch {BranchName} from {BaseBranch}", branchName, request.BaseBranch);

            // Step 5: Commit all files (multi-file atomic commit)
            var commitMessage = string.IsNullOrEmpty(request.CommitMessage)
                ? $"chore({request.WorkflowType}): {request.Title}"
                : request.CommitMessage;

            commitSha = await _commitService.CreateCommitAsync(
                request.RepositoryOwner,
                request.RepositoryName,
                branchName,
                commitMessage,
                request.Files);

            _logger.LogInformation(
                "Created commit {CommitSha} on branch {BranchName} with {FileCount} files",
                commitSha, branchName, request.Files.Count);

            // Step 6: Generate PR description from template
            var prDescription = GeneratePRDescription(request);

            // Step 7: Create pull request
            var prRequest = new CreatePullRequestRequest
            {
                Title = request.Title,
                Body = prDescription,
                HeadBranch = branchName,
                BaseBranch = request.BaseBranch,
                Reviewers = request.Reviewers,
                Labels = request.Labels,
                Draft = request.Draft,
                MaintainerCanModify = true
            };

            var prResponse = await _pullRequestService.CreatePullRequestAsync(
                request.RepositoryOwner,
                request.RepositoryName,
                prRequest,
                request.TenantId);

            _logger.LogInformation(
                "Created PR #{PrNumber}: {PrUrl}",
                prResponse.Number, prResponse.HtmlUrl);

            // Step 8: Store PR in database
            var autonomousPR = new AutonomousPullRequest
            {
                Id = Guid.NewGuid(),
                TenantId = Guid.Parse(request.TenantId),
                PrNumber = prResponse.Number,
                RepositoryName = $"{request.RepositoryOwner}/{request.RepositoryName}",
                BranchName = branchName,
                Title = request.Title,
                Description = prDescription,
                WorkflowType = request.WorkflowType.ToString(),
                Status = "open",
                CreatedAt = DateTime.UtcNow
            };

            var savedPR = await _repository.CreateAsync(autonomousPR);

            _logger.LogInformation("Saved autonomous PR to database with ID: {PrId}", savedPR.Id);

            // Step 9: Publish Kafka event (autonomous.pr.created.v1)
            await PublishPRCreatedEventAsync(savedPR, prResponse, commitSha, request.Files.Count, request.AutoMerge, correlationId);

            // Step 9.5: Send notifications
            await SendPRCreatedNotificationsAsync(savedPR, prResponse, request);

            // Step 10: Handle auto-merge if requested
            if (request.AutoMerge)
            {
                _logger.LogInformation("Auto-merge requested for PR #{PrNumber}. Will merge after CI checks.", prResponse.Number);
                // Note: Auto-merge implementation would require monitoring CI status
                // This is a placeholder for future implementation
                // await WaitForCIAndMergeAsync(request, prResponse.Number, savedPR.Id.ToString());
            }

            // Return success response
            return new CreateAutonomousPRResponse
            {
                PrId = savedPR.Id.ToString(),
                PrNumber = prResponse.Number,
                PrUrl = prResponse.HtmlUrl,
                BranchName = branchName,
                Status = "open",
                CreatedAt = savedPR.CreatedAt,
                Success = true,
                CommitSha = commitSha,
                FilesChanged = request.Files.Count,
                ReviewersRequested = request.Reviewers.Any(),
                LabelsAdded = request.Labels.Any()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to create autonomous PR. Repository: {Owner}/{Repo}, WorkflowType: {WorkflowType}, CorrelationId: {CorrelationId}",
                request.RepositoryOwner, request.RepositoryName, request.WorkflowType, correlationId);

            // Publish failure event
            await PublishPRFailedEventAsync(request, ex, branchName, commitSha, correlationId);

            // Send failure notification
            await SendPRFailedNotificationsAsync(request, ex.Message);

            // Attempt rollback if branch was created
            if (branchName != null)
            {
                try
                {
                    var exists = await _branchService.BranchExistsAsync(
                        request.RepositoryOwner,
                        request.RepositoryName,
                        branchName);

                    if (exists)
                    {
                        await _branchService.DeleteBranchAsync(
                            request.RepositoryOwner,
                            request.RepositoryName,
                            branchName);
                        _logger.LogInformation("Rolled back branch {BranchName} after failure", branchName);
                    }
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(rollbackEx, "Failed to rollback branch {BranchName}", branchName);
                }
            }

            // Return failure response
            return new CreateAutonomousPRResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                BranchName = branchName ?? string.Empty,
                Status = "failed",
                CreatedAt = DateTime.UtcNow,
                CommitSha = commitSha
            };
        }
    }

    /// <summary>
    /// Get the status of an autonomous pull request
    /// </summary>
    public async Task<CreateAutonomousPRResponse> GetPRStatusAsync(string tenantId, string prId)
    {
        _logger.LogInformation("Getting status for autonomous PR {PrId}, Tenant: {TenantId}", prId, tenantId);

        var pr = await _repository.GetByIdAsync(Guid.Parse(prId));

        if (pr == null || pr.TenantId.ToString() != tenantId)
        {
            throw new InvalidOperationException($"Autonomous PR {prId} not found for tenant {tenantId}");
        }

        // Parse repository owner/name
        var repoParts = pr.RepositoryName.Split('/');
        if (repoParts.Length != 2)
        {
            throw new InvalidOperationException($"Invalid repository name format: {pr.RepositoryName}");
        }

        var owner = repoParts[0];
        var repo = repoParts[1];

        // Get latest PR status from GitHub
        try
        {
            var githubPR = await _pullRequestService.GetPullRequestAsync(owner, repo, pr.PrNumber, tenantId);

            // Update database if status changed
            if (githubPR.State != pr.Status)
            {
                await _repository.UpdateStatusAsync(
                    pr.Id,
                    githubPR.State,
                    githubPR.MergedAt);
                pr.Status = githubPR.State;
                pr.MergedAt = githubPR.MergedAt;
            }

            return new CreateAutonomousPRResponse
            {
                PrId = pr.Id.ToString(),
                PrNumber = pr.PrNumber,
                PrUrl = githubPR.HtmlUrl,
                BranchName = pr.BranchName,
                Status = pr.Status,
                CreatedAt = pr.CreatedAt,
                Success = true,
                FilesChanged = githubPR.ChangedFiles
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get PR status from GitHub for PR #{PrNumber}", pr.PrNumber);

            // Return cached database status
            return new CreateAutonomousPRResponse
            {
                PrId = pr.Id.ToString(),
                PrNumber = pr.PrNumber,
                PrUrl = string.Empty,
                BranchName = pr.BranchName,
                Status = pr.Status,
                CreatedAt = pr.CreatedAt,
                Success = true,
                ErrorMessage = "Status from database cache (GitHub API unavailable)"
            };
        }
    }

    /// <summary>
    /// List all autonomous pull requests for a tenant
    /// </summary>
    public async Task<IEnumerable<AutonomousPullRequest>> ListAutonomousPRsAsync(string tenantId, string? status = null)
    {
        _logger.LogInformation("Listing autonomous PRs for tenant {TenantId}, Status filter: {Status}", tenantId, status ?? "all");

        var tenantGuid = Guid.Parse(tenantId);

        if (string.IsNullOrEmpty(status))
        {
            return await _repository.GetByTenantAsync(tenantGuid);
        }

        if (status.Equals("open", StringComparison.OrdinalIgnoreCase))
        {
            return await _repository.GetOpenByTenantAsync(tenantGuid);
        }

        // For other statuses, get all and filter
        var allPRs = await _repository.GetByTenantAsync(tenantGuid);
        return allPRs.Where(pr => pr.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Retry a failed autonomous pull request
    /// </summary>
    public async Task<CreateAutonomousPRResponse> RetryFailedPRAsync(string tenantId, string prId)
    {
        _logger.LogInformation("Retrying failed autonomous PR {PrId}, Tenant: {TenantId}", prId, tenantId);

        var pr = await _repository.GetByIdAsync(Guid.Parse(prId));

        if (pr == null || pr.TenantId.ToString() != tenantId)
        {
            throw new InvalidOperationException($"Autonomous PR {prId} not found for tenant {tenantId}");
        }

        if (pr.Status != "failed" && pr.Status != "closed")
        {
            throw new InvalidOperationException($"PR {prId} is in status '{pr.Status}' and cannot be retried");
        }

        // Note: For retry, we would need to reconstruct the original request
        // This is a simplified implementation
        throw new NotImplementedException("Retry functionality requires storing original request data");
    }

    /// <summary>
    /// Close an autonomous pull request without merging
    /// </summary>
    public async Task<bool> CloseAutonomousPRAsync(string tenantId, string prId)
    {
        _logger.LogInformation("Closing autonomous PR {PrId}, Tenant: {TenantId}", prId, tenantId);

        var pr = await _repository.GetByIdAsync(Guid.Parse(prId));

        if (pr == null || pr.TenantId.ToString() != tenantId)
        {
            throw new InvalidOperationException($"Autonomous PR {prId} not found for tenant {tenantId}");
        }

        // Parse repository owner/name
        var repoParts = pr.RepositoryName.Split('/');
        if (repoParts.Length != 2)
        {
            throw new InvalidOperationException($"Invalid repository name format: {pr.RepositoryName}");
        }

        var owner = repoParts[0];
        var repo = repoParts[1];

        // Close PR on GitHub
        var closed = await _pullRequestService.ClosePullRequestAsync(owner, repo, pr.PrNumber, tenantId);

        if (closed)
        {
            // Update database
            await _repository.UpdateStatusAsync(pr.Id, "closed");
            _logger.LogInformation("Successfully closed PR #{PrNumber}", pr.PrNumber);
        }

        return closed;
    }

    /// <summary>
    /// Merge an autonomous pull request
    /// </summary>
    public async Task<bool> MergeAutonomousPRAsync(string tenantId, string prId, string? commitMessage = null)
    {
        _logger.LogInformation("Merging autonomous PR {PrId}, Tenant: {TenantId}", prId, tenantId);

        var pr = await _repository.GetByIdAsync(Guid.Parse(prId));

        if (pr == null || pr.TenantId.ToString() != tenantId)
        {
            throw new InvalidOperationException($"Autonomous PR {prId} not found for tenant {tenantId}");
        }

        // Parse repository owner/name
        var repoParts = pr.RepositoryName.Split('/');
        if (repoParts.Length != 2)
        {
            throw new InvalidOperationException($"Invalid repository name format: {pr.RepositoryName}");
        }

        var owner = repoParts[0];
        var repo = repoParts[1];

        // Merge PR on GitHub
        var mergeRequest = new MergePullRequestRequest
        {
            CommitMessage = commitMessage ?? $"Merge PR #{pr.PrNumber}: {pr.Title}",
            MergeMethod = "squash"
        };

        var merged = await _pullRequestService.MergePullRequestAsync(owner, repo, pr.PrNumber, mergeRequest, tenantId);

        if (merged)
        {
            // Update database
            var mergedAt = DateTime.UtcNow;
            await _repository.UpdateStatusAsync(pr.Id, "merged", mergedAt);

            // Publish merged event
            await PublishPRMergedEventAsync(pr, mergedAt, false, Guid.NewGuid().ToString());

            // Send merged notification
            await SendPRMergedNotificationsAsync(pr);

            _logger.LogInformation("Successfully merged PR #{PrNumber}", pr.PrNumber);
        }

        return merged;
    }

    #region Private Helper Methods

    /// <summary>
    /// Generate unique branch name with timestamp and random suffix
    /// Format: {prefix}-{timestamp}-{random}
    /// Example: claude/autonomous-20251115-abc123
    /// </summary>
    private string GenerateUniqueBranchName(string prefix)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var random = Guid.NewGuid().ToString("N")[..6];
        return $"{prefix}-{timestamp}-{random}";
    }

    /// <summary>
    /// Generate PR description based on workflow type
    /// </summary>
    private string GeneratePRDescription(CreateAutonomousPRRequest request)
    {
        var filePaths = request.Files.Select(f => f.Path).ToList();

        return request.WorkflowType switch
        {
            WorkflowType.OntologyRefactoring => _templateService.GenerateOntologyRefactoringDescription(
                request.TemplateData.GetValueOrDefault("entityName", "Unknown"),
                int.Parse(request.TemplateData.GetValueOrDefault("addedProperties", "0")),
                int.Parse(request.TemplateData.GetValueOrDefault("updatedRelationships", "0")),
                int.Parse(request.TemplateData.GetValueOrDefault("refactoredValidators", "0")),
                filePaths),

            WorkflowType.CodeGeneration => _templateService.GenerateCodeGenerationDescription(
                request.TemplateData.GetValueOrDefault("generatedComponent", "Unknown"),
                filePaths,
                request.TemplateData.GetValueOrDefault("additionalNotes")),

            WorkflowType.BugFix => _templateService.GenerateBugFixDescription(
                request.TemplateData.GetValueOrDefault("bugDescription", "Bug fix"),
                request.TemplateData.GetValueOrDefault("fixDescription", "Fixed issue"),
                filePaths,
                request.TemplateData.GetValueOrDefault("issueNumber")),

            WorkflowType.FeatureAddition => _templateService.GenerateFeatureAdditionDescription(
                request.TemplateData.GetValueOrDefault("featureName", "New feature"),
                request.TemplateData.GetValueOrDefault("featureDescription", "Added feature"),
                filePaths,
                request.TemplateData.GetValueOrDefault("apiEndpoints", "").Split(',').ToList(),
                bool.Parse(request.TemplateData.GetValueOrDefault("requiresDatabaseMigration", "false"))),

            WorkflowType.Refactoring => _templateService.GenerateRefactoringDescription(
                request.TemplateData.GetValueOrDefault("refactoringScope", "Code refactoring"),
                request.TemplateData.GetValueOrDefault("refactoringReason", "Improved code quality"),
                filePaths,
                bool.Parse(request.TemplateData.GetValueOrDefault("breakingChanges", "false"))),

            WorkflowType.Documentation or _ => _templateService.GenerateGeneralDescription(
                request.Title,
                request.TemplateData.GetValueOrDefault("description", "Autonomous PR"),
                filePaths)
        };
    }

    /// <summary>
    /// Publish PR created event to Kafka
    /// </summary>
    private async Task PublishPRCreatedEventAsync(
        AutonomousPullRequest pr,
        PullRequestResponse prResponse,
        string? commitSha,
        int filesChanged,
        bool autoMerge,
        string correlationId)
    {
        var eventData = new AutonomousPRCreatedEvent
        {
            EventId = Guid.NewGuid().ToString(),
            TenantId = pr.TenantId.ToString(),
            CorrelationId = correlationId,
            Payload = new AutonomousPRCreatedPayload
            {
                PrId = pr.Id.ToString(),
                PrNumber = pr.PrNumber,
                RepositoryName = pr.RepositoryName,
                BranchName = pr.BranchName,
                Title = pr.Title,
                WorkflowType = pr.WorkflowType,
                PrUrl = prResponse.HtmlUrl,
                FilesChanged = filesChanged,
                CommitSha = commitSha,
                AutoMerge = autoMerge
            }
        };

        await _kafkaProducer.ProduceAsync(TopicPRCreated, eventData);
        _logger.LogInformation("Published PR created event to Kafka topic {Topic}", TopicPRCreated);
    }

    /// <summary>
    /// Publish PR merged event to Kafka
    /// </summary>
    private async Task PublishPRMergedEventAsync(
        AutonomousPullRequest pr,
        DateTime mergedAt,
        bool autoMerged,
        string correlationId)
    {
        var eventData = new AutonomousPRMergedEvent
        {
            EventId = Guid.NewGuid().ToString(),
            TenantId = pr.TenantId.ToString(),
            CorrelationId = correlationId,
            Payload = new AutonomousPRMergedPayload
            {
                PrId = pr.Id.ToString(),
                PrNumber = pr.PrNumber,
                RepositoryName = pr.RepositoryName,
                BranchName = pr.BranchName,
                Title = pr.Title,
                WorkflowType = pr.WorkflowType,
                MergedAt = mergedAt,
                AutoMerged = autoMerged
            }
        };

        await _kafkaProducer.ProduceAsync(TopicPRMerged, eventData);
        _logger.LogInformation("Published PR merged event to Kafka topic {Topic}", TopicPRMerged);
    }

    /// <summary>
    /// Publish PR failed event to Kafka
    /// </summary>
    private async Task PublishPRFailedEventAsync(
        CreateAutonomousPRRequest request,
        Exception ex,
        string? branchName,
        string? commitSha,
        string correlationId)
    {
        var failureStage = DetermineFailureStage(ex, branchName, commitSha);

        var eventData = new AutonomousPRFailedEvent
        {
            EventId = Guid.NewGuid().ToString(),
            TenantId = request.TenantId,
            CorrelationId = correlationId,
            Payload = new AutonomousPRFailedPayload
            {
                RepositoryName = $"{request.RepositoryOwner}/{request.RepositoryName}",
                BranchName = branchName,
                Title = request.Title,
                WorkflowType = request.WorkflowType.ToString(),
                ErrorMessage = ex.Message,
                FailureStage = failureStage,
                ExceptionType = ex.GetType().Name,
                FilesChanged = request.Files.Count,
                IsRetry = false,
                RetryCount = 0
            }
        };

        await _kafkaProducer.ProduceAsync(TopicPRFailed, eventData);
        _logger.LogInformation("Published PR failed event to Kafka topic {Topic}", TopicPRFailed);
    }

    /// <summary>
    /// Determine failure stage based on exception and state
    /// </summary>
    private string DetermineFailureStage(Exception ex, string? branchName, string? commitSha)
    {
        if (branchName == null)
            return "branch_creation";

        if (commitSha == null)
            return "commit";

        return "pr_creation";
    }

    /// <summary>
    /// Send notifications for PR created event
    /// </summary>
    private async Task SendPRCreatedNotificationsAsync(
        AutonomousPullRequest pr,
        PullRequestResponse prResponse,
        CreateAutonomousPRRequest request)
    {
        try
        {
            var prData = new PullRequestNotificationData
            {
                PrNumber = pr.PrNumber,
                Title = pr.Title,
                Repository = pr.RepositoryName,
                Url = prResponse.HtmlUrl,
                Status = "open",
                WorkflowType = pr.WorkflowType.ToLower().Replace(" ", "_"),
                BranchName = pr.BranchName,
                Creator = "Binelek Platform",
                Reviewers = request.Reviewers,
                CommitCount = 1,
                FileCount = request.Files.Count
            };

            var recipients = request.Reviewers.Any() ? request.Reviewers : new List<string> { "team@binelek.com" };
            await _notificationService.NotifyPRCreatedAsync(prData, recipients);

            _logger.LogInformation("Sent PR created notifications for PR #{PrNumber}", pr.PrNumber);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send PR created notifications for PR #{PrNumber}", pr.PrNumber);
            // Don't throw - notifications are not critical
        }
    }

    /// <summary>
    /// Send notifications for PR merged event
    /// </summary>
    private async Task SendPRMergedNotificationsAsync(AutonomousPullRequest pr)
    {
        try
        {
            var prData = new PullRequestNotificationData
            {
                PrNumber = pr.PrNumber,
                Title = pr.Title,
                Repository = pr.RepositoryName,
                Url = $"https://github.com/{pr.RepositoryName}/pull/{pr.PrNumber}",
                Status = "merged",
                WorkflowType = pr.WorkflowType.ToLower().Replace(" ", "_"),
                BranchName = pr.BranchName,
                Creator = "Binelek Platform"
            };

            var recipients = new List<string> { "team@binelek.com" };
            await _notificationService.NotifyPRMergedAsync(prData, recipients);

            _logger.LogInformation("Sent PR merged notifications for PR #{PrNumber}", pr.PrNumber);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send PR merged notifications for PR #{PrNumber}", pr.PrNumber);
            // Don't throw - notifications are not critical
        }
    }

    /// <summary>
    /// Send notifications for PR failed event
    /// </summary>
    private async Task SendPRFailedNotificationsAsync(CreateAutonomousPRRequest request, string error)
    {
        try
        {
            var prData = new PullRequestNotificationData
            {
                PrNumber = 0,
                Title = request.Title,
                Repository = $"{request.RepositoryOwner}/{request.RepositoryName}",
                Url = "",
                Status = "failed",
                WorkflowType = request.WorkflowType.ToString().ToLower().Replace(" ", "_"),
                Error = error,
                Creator = "Binelek Platform"
            };

            var recipients = request.Reviewers.Any() ? request.Reviewers : new List<string> { "team@binelek.com" };
            await _notificationService.NotifyPRFailedAsync(prData, error, recipients);

            _logger.LogInformation("Sent PR failed notifications for title: {Title}", request.Title);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send PR failed notifications for title: {Title}", request.Title);
            // Don't throw - notifications are not critical
        }
    }

    #endregion
}

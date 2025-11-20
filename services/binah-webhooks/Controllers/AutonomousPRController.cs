using Binah.Contracts.Common;
using Binah.Webhooks.Models.DTOs.GitHub;
using Binah.Webhooks.Models.Domain;
using Binah.Webhooks.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Binah.Webhooks.Controllers;

/// <summary>
/// Controller for autonomous pull request operations
/// Orchestrates the complete workflow: branch creation, file commits, PR creation, and tracking
/// </summary>
[ApiController]
[Route("api/github/pr")]
[Authorize]
public class AutonomousPRController : ControllerBase
{
    private readonly IAutonomousPRService _autonomousPRService;
    private readonly ILogger<AutonomousPRController> _logger;

    public AutonomousPRController(
        IAutonomousPRService autonomousPRService,
        ILogger<AutonomousPRController> logger)
    {
        _autonomousPRService = autonomousPRService ?? throw new ArgumentNullException(nameof(autonomousPRService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Create an autonomous pull request
    /// </summary>
    /// <remarks>
    /// This endpoint orchestrates the complete autonomous PR workflow:
    /// 1. Generate unique branch name
    /// 2. Create branch from base branch
    /// 3. Commit all files to the new branch
    /// 4. Generate PR description from template
    /// 5. Create pull request on GitHub
    /// 6. Store PR metadata in database
    /// 7. Publish Kafka event (autonomous.pr.created.v1)
    /// 8. Optionally enable auto-merge when CI passes
    ///
    /// **Example Request:**
    /// ```json
    /// {
    ///   "tenantId": "550e8400-e29b-41d4-a716-446655440000",
    ///   "repositoryOwner": "k5tuck",
    ///   "repositoryName": "Binelek",
    ///   "baseBranch": "main",
    ///   "branchPrefix": "claude/auto-refactor",
    ///   "title": "Refactor Property entity with new energy score attribute",
    ///   "workflowType": "OntologyRefactoring",
    ///   "files": [
    ///     {
    ///       "path": "schemas/core-real-estate-ontology.yaml",
    ///       "content": "...",
    ///       "mode": "Update"
    ///     },
    ///     {
    ///       "path": "services/binah-ontology/Models/Property.cs",
    ///       "content": "...",
    ///       "mode": "Update"
    ///     }
    ///   ],
    ///   "templateData": {
    ///     "EntityName": "Property",
    ///     "AddedProperties": "1",
    ///     "UpdatedRelationships": "0",
    ///     "RefactoredValidators": "1"
    ///   },
    ///   "commitMessage": "refactor(ontology): Add energy score to Property entity",
    ///   "reviewers": ["k5tuck"],
    ///   "labels": ["auto-generated", "ontology"],
    ///   "draft": false,
    ///   "autoMerge": false
    /// }
    /// ```
    /// </remarks>
    /// <param name="request">Autonomous PR creation request</param>
    /// <returns>PR creation response with PR number and URL</returns>
    [HttpPost("create")]
    [ProducesResponseType(typeof(ApiResponse<CreateAutonomousPRResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CreateAutonomousPRResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<CreateAutonomousPRResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<CreateAutonomousPRResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<CreateAutonomousPRResponse>>> CreateAutonomousPR(
        [FromBody] CreateAutonomousPRRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Received autonomous PR creation request for {Repository}, workflow type: {WorkflowType}",
                $"{request.RepositoryOwner}/{request.RepositoryName}",
                request.WorkflowType);

            // Validate request
            if (string.IsNullOrEmpty(request.TenantId))
            {
                return BadRequest(ApiResponse<CreateAutonomousPRResponse>.WithError("TenantId is required"));
            }

            if (string.IsNullOrEmpty(request.RepositoryOwner) || string.IsNullOrEmpty(request.RepositoryName))
            {
                return BadRequest(ApiResponse<CreateAutonomousPRResponse>.WithError("Repository owner and name are required"));
            }

            if (string.IsNullOrEmpty(request.Title))
            {
                return BadRequest(ApiResponse<CreateAutonomousPRResponse>.WithError("PR title is required"));
            }

            if (request.Files == null || !request.Files.Any())
            {
                return BadRequest(ApiResponse<CreateAutonomousPRResponse>.WithError("At least one file change is required"));
            }

            if (string.IsNullOrEmpty(request.CommitMessage))
            {
                return BadRequest(ApiResponse<CreateAutonomousPRResponse>.WithError("Commit message is required"));
            }

            // Extract tenant ID from JWT token and verify it matches request
            var tokenTenantId = User.FindFirst("tenant_id")?.Value;
            if (tokenTenantId != request.TenantId)
            {
                _logger.LogWarning(
                    "Tenant ID mismatch: token={TokenTenantId}, request={RequestTenantId}",
                    tokenTenantId,
                    request.TenantId);
                return Unauthorized(ApiResponse<CreateAutonomousPRResponse>.WithError("Tenant ID mismatch"));
            }

            // Create autonomous PR
            var response = await _autonomousPRService.CreateAutonomousPRAsync(request);

            if (!response.Success)
            {
                _logger.LogError("Autonomous PR creation failed: {ErrorMessage}", response.ErrorMessage);
                return StatusCode(500, ApiResponse<CreateAutonomousPRResponse>.WithError(response.ErrorMessage ?? "Unknown error"));
            }

            _logger.LogInformation(
                "Successfully created autonomous PR #{PrNumber}: {PrUrl}",
                response.PrNumber,
                response.PrUrl);

            return Ok(ApiResponse<CreateAutonomousPRResponse>.Ok(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating autonomous PR");
            return StatusCode(500, ApiResponse<CreateAutonomousPRResponse>.WithError("Internal server error"));
        }
    }

    /// <summary>
    /// Get the status of an autonomous pull request
    /// </summary>
    /// <param name="prId">Pull request database ID (GUID)</param>
    /// <returns>Current PR status</returns>
    [HttpGet("{prId}/status")]
    [ProducesResponseType(typeof(ApiResponse<CreateAutonomousPRResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CreateAutonomousPRResponse>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CreateAutonomousPRResponse>>> GetPRStatus(string prId)
    {
        try
        {
            var tenantId = User.FindFirst("tenant_id")?.Value;
            if (string.IsNullOrEmpty(tenantId))
            {
                return Unauthorized(ApiResponse<CreateAutonomousPRResponse>.WithError("Tenant ID not found in token"));
            }

            var response = await _autonomousPRService.GetPRStatusAsync(tenantId, prId);

            if (!response.Success)
            {
                return NotFound(ApiResponse<CreateAutonomousPRResponse>.WithError(response.ErrorMessage ?? "PR not found"));
            }

            return Ok(ApiResponse<CreateAutonomousPRResponse>.Ok(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting PR status for {PrId}", prId);
            return StatusCode(500, ApiResponse<CreateAutonomousPRResponse>.WithError("Internal server error"));
        }
    }

    /// <summary>
    /// List all autonomous pull requests for the current tenant
    /// </summary>
    /// <param name="status">Optional status filter (open, merged, closed)</param>
    /// <returns>List of autonomous pull requests</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<AutonomousPullRequest>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<AutonomousPullRequest>>>> ListAutonomousPRs(
        [FromQuery] string? status = null)
    {
        try
        {
            var tenantId = User.FindFirst("tenant_id")?.Value;
            if (string.IsNullOrEmpty(tenantId))
            {
                return Unauthorized(ApiResponse<IEnumerable<AutonomousPullRequest>>.WithError("Tenant ID not found in token"));
            }

            var prs = await _autonomousPRService.ListAutonomousPRsAsync(tenantId, status);

            return Ok(ApiResponse<IEnumerable<AutonomousPullRequest>>.Ok(prs));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing autonomous PRs");
            return StatusCode(500, ApiResponse<IEnumerable<AutonomousPullRequest>>.WithError("Internal server error"));
        }
    }

    /// <summary>
    /// Close an autonomous pull request without merging
    /// </summary>
    /// <param name="prId">Pull request database ID (GUID)</param>
    /// <returns>True if closed successfully</returns>
    [HttpPost("{prId}/close")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> CloseAutonomousPR(string prId)
    {
        try
        {
            var tenantId = User.FindFirst("tenant_id")?.Value;
            if (string.IsNullOrEmpty(tenantId))
            {
                return Unauthorized(ApiResponse<bool>.WithError("Tenant ID not found in token"));
            }

            var closed = await _autonomousPRService.CloseAutonomousPRAsync(tenantId, prId);

            if (!closed)
            {
                return NotFound(ApiResponse<bool>.WithError("Failed to close PR or PR not found"));
            }

            return Ok(ApiResponse<bool>.Ok(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing autonomous PR {PrId}", prId);
            return StatusCode(500, ApiResponse<bool>.WithError("Internal server error"));
        }
    }

    /// <summary>
    /// Merge an autonomous pull request
    /// </summary>
    /// <param name="prId">Pull request database ID (GUID)</param>
    /// <param name="commitMessage">Optional custom merge commit message</param>
    /// <returns>True if merged successfully</returns>
    [HttpPost("{prId}/merge")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> MergeAutonomousPR(
        string prId,
        [FromBody] string? commitMessage = null)
    {
        try
        {
            var tenantId = User.FindFirst("tenant_id")?.Value;
            if (string.IsNullOrEmpty(tenantId))
            {
                return Unauthorized(ApiResponse<bool>.WithError("Tenant ID not found in token"));
            }

            var merged = await _autonomousPRService.MergeAutonomousPRAsync(tenantId, prId, commitMessage);

            if (!merged)
            {
                return NotFound(ApiResponse<bool>.WithError("Failed to merge PR or PR not found"));
            }

            return Ok(ApiResponse<bool>.Ok(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error merging autonomous PR {PrId}", prId);
            return StatusCode(500, ApiResponse<bool>.WithError("Internal server error"));
        }
    }

    /// <summary>
    /// Retry a failed autonomous pull request
    /// </summary>
    /// <param name="prId">Pull request database ID (GUID)</param>
    /// <returns>Updated PR creation response</returns>
    [HttpPost("{prId}/retry")]
    [ProducesResponseType(typeof(ApiResponse<CreateAutonomousPRResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CreateAutonomousPRResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<CreateAutonomousPRResponse>), StatusCodes.Status501NotImplemented)]
    public async Task<ActionResult<ApiResponse<CreateAutonomousPRResponse>>> RetryFailedPR(string prId)
    {
        try
        {
            var tenantId = User.FindFirst("tenant_id")?.Value;
            if (string.IsNullOrEmpty(tenantId))
            {
                return Unauthorized(ApiResponse<CreateAutonomousPRResponse>.WithError("Tenant ID not found in token"));
            }

            var response = await _autonomousPRService.RetryFailedPRAsync(tenantId, prId);

            return Ok(ApiResponse<CreateAutonomousPRResponse>.Ok(response));
        }
        catch (NotImplementedException)
        {
            return StatusCode(501, ApiResponse<CreateAutonomousPRResponse>.WithError("Retry functionality not yet implemented"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying failed PR {PrId}", prId);
            return StatusCode(500, ApiResponse<CreateAutonomousPRResponse>.WithError("Internal server error"));
        }
    }
}

using Binah.Webhooks.Services.Interfaces;
using Binah.Contracts.Common;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace Binah.Webhooks.Controllers;

/// <summary>
/// Controller for GitHub webhook events
/// Receives and processes webhooks from GitHub with HMAC-SHA256 signature verification
/// </summary>
[ApiController]
[Route("api/github")]
public class GitHubWebhookController : ControllerBase
{
    private readonly IGitHubWebhookService _githubWebhookService;
    private readonly ILogger<GitHubWebhookController> _logger;
    private readonly IConfiguration _configuration;

    public GitHubWebhookController(
        IGitHubWebhookService githubWebhookService,
        ILogger<GitHubWebhookController> logger,
        IConfiguration configuration)
    {
        _githubWebhookService = githubWebhookService ?? throw new ArgumentNullException(nameof(githubWebhookService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Receive GitHub webhook events
    /// </summary>
    /// <remarks>
    /// This endpoint receives webhook events from GitHub and verifies the signature
    /// using HMAC-SHA256. The webhook secret must be configured in appsettings.json
    /// under GitHub:WebhookSecret.
    ///
    /// GitHub sends the following headers:
    /// - X-GitHub-Event: Event type (push, pull_request, issues, etc.)
    /// - X-Hub-Signature-256: HMAC-SHA256 signature for verification
    /// - X-GitHub-Delivery: Unique delivery ID for this webhook
    /// </remarks>
    [HttpPost("webhook")]
    [ProducesResponseType(typeof(ApiResponse<GitHubWebhookResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<GitHubWebhookResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<GitHubWebhookResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<GitHubWebhookResponse>>> ReceiveWebhook()
    {
        try
        {
            // Extract GitHub headers
            var eventType = Request.Headers["X-GitHub-Event"].FirstOrDefault();
            var signature = Request.Headers["X-Hub-Signature-256"].FirstOrDefault();
            var deliveryId = Request.Headers["X-GitHub-Delivery"].FirstOrDefault();

            if (string.IsNullOrEmpty(eventType))
            {
                _logger.LogWarning("GitHub webhook rejected: X-GitHub-Event header is missing");
                return BadRequest(ApiResponse<GitHubWebhookResponse>.WithError("X-GitHub-Event header is required"));
            }

            if (string.IsNullOrEmpty(signature))
            {
                _logger.LogWarning("GitHub webhook rejected: X-Hub-Signature-256 header is missing");
                return Unauthorized(ApiResponse<GitHubWebhookResponse>.WithError("X-Hub-Signature-256 header is required"));
            }

            if (string.IsNullOrEmpty(deliveryId))
            {
                _logger.LogWarning("GitHub webhook rejected: X-GitHub-Delivery header is missing");
                return BadRequest(ApiResponse<GitHubWebhookResponse>.WithError("X-GitHub-Delivery header is required"));
            }

            // Read the raw payload
            string payload;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                payload = await reader.ReadToEndAsync();
            }

            if (string.IsNullOrEmpty(payload))
            {
                _logger.LogWarning("GitHub webhook rejected: payload is empty");
                return BadRequest(ApiResponse<GitHubWebhookResponse>.WithError("Payload cannot be empty"));
            }

            // Get the webhook secret from configuration
            var secret = _configuration["GitHub:WebhookSecret"];
            if (string.IsNullOrEmpty(secret))
            {
                _logger.LogError("GitHub webhook secret is not configured");
                return StatusCode(500, ApiResponse<GitHubWebhookResponse>.WithError("Webhook secret is not configured"));
            }

            // Verify the signature
            var isValidSignature = _githubWebhookService.VerifyGitHubSignature(payload, signature, secret);

            if (!isValidSignature)
            {
                _logger.LogWarning(
                    "GitHub webhook signature verification failed - EventType: {EventType}, DeliveryId: {DeliveryId}",
                    eventType, deliveryId);

                return Unauthorized(ApiResponse<GitHubWebhookResponse>.WithError("Invalid signature"));
            }

            // Extract tenant ID from JWT token (if authenticated) or use default
            var tenantId = User.FindFirst("tenant_id")?.Value ?? "default";

            // Process the webhook
            var processed = await _githubWebhookService.ProcessWebhookAsync(eventType, deliveryId, payload, tenantId);

            if (!processed)
            {
                _logger.LogWarning(
                    "GitHub webhook processing failed - EventType: {EventType}, DeliveryId: {DeliveryId}",
                    eventType, deliveryId);

                return StatusCode(500, ApiResponse<GitHubWebhookResponse>.WithError("Webhook processing failed"));
            }

            _logger.LogInformation(
                "GitHub webhook received and queued - EventType: {EventType}, DeliveryId: {DeliveryId}, TenantId: {TenantId}",
                eventType, deliveryId, tenantId);

            var response = new GitHubWebhookResponse
            {
                Success = true,
                EventType = eventType,
                DeliveryId = deliveryId,
                Message = "Webhook received and queued for processing"
            };

            return Ok(ApiResponse<GitHubWebhookResponse>.Ok(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing GitHub webhook");
            return StatusCode(500, ApiResponse<GitHubWebhookResponse>.WithError("Internal server error"));
        }
    }
}

/// <summary>
/// GitHub webhook response
/// </summary>
public class GitHubWebhookResponse
{
    public bool Success { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string DeliveryId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

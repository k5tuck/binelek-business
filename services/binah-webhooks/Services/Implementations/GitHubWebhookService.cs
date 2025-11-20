using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Binah.Webhooks.Models.Domain;
using Binah.Webhooks.Models.DTOs.GitHub;
using Binah.Webhooks.Repositories.Interfaces;
using Binah.Webhooks.Services.Interfaces;

namespace Binah.Webhooks.Services.Implementations;

/// <summary>
/// GitHub webhook service implementation
/// Handles signature verification and webhook processing
/// </summary>
public class GitHubWebhookService : IGitHubWebhookService
{
    private readonly ILogger<GitHubWebhookService> _logger;
    private readonly IGitHubWebhookEventRepository _repository;
    private readonly IGitHubEventPublisher _eventPublisher;

    public GitHubWebhookService(
        ILogger<GitHubWebhookService> logger,
        IGitHubWebhookEventRepository repository,
        IGitHubEventPublisher eventPublisher)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
    }

    /// <summary>
    /// Verify GitHub webhook signature using HMAC-SHA256
    /// Uses constant-time comparison to prevent timing attacks
    /// </summary>
    public bool VerifyGitHubSignature(string payload, string signature, string secret)
    {
        if (string.IsNullOrEmpty(payload))
        {
            _logger.LogWarning("GitHub webhook verification failed: payload is empty");
            return false;
        }

        if (string.IsNullOrEmpty(signature))
        {
            _logger.LogWarning("GitHub webhook verification failed: signature is empty");
            return false;
        }

        if (string.IsNullOrEmpty(secret))
        {
            _logger.LogError("GitHub webhook verification failed: secret is not configured");
            return false;
        }

        try
        {
            var encoding = new UTF8Encoding();
            var keyBytes = encoding.GetBytes(secret);
            var payloadBytes = encoding.GetBytes(payload);

            using (var hmac = new HMACSHA256(keyBytes))
            {
                var hash = hmac.ComputeHash(payloadBytes);
                var expectedSignature = "sha256=" + BitConverter.ToString(hash).Replace("-", "").ToLower();

                // Use constant-time comparison to prevent timing attacks
                var isValid = ConstantTimeEquals(signature, expectedSignature);

                if (isValid)
                {
                    _logger.LogInformation("GitHub webhook signature verified successfully");
                }
                else
                {
                    _logger.LogWarning("GitHub webhook signature verification failed: signature mismatch");
                }

                return isValid;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during GitHub webhook signature verification");
            return false;
        }
    }

    /// <summary>
    /// Process GitHub webhook event
    /// 1. Parse the payload
    /// 2. Store event in database
    /// 3. Publish to Kafka for downstream processing
    /// 4. Mark as processed
    /// </summary>
    public async Task<bool> ProcessWebhookAsync(string eventType, string deliveryId, string payload, string tenantId)
    {
        _logger.LogInformation(
            "Processing GitHub webhook - EventType: {EventType}, DeliveryId: {DeliveryId}, TenantId: {TenantId}",
            eventType, deliveryId, tenantId);

        try
        {
            // Parse tenant ID
            if (!Guid.TryParse(tenantId, out var tenantGuid))
            {
                _logger.LogError("Invalid tenant ID format: {TenantId}", tenantId);
                return false;
            }

            // Try to parse the payload as GitHubWebhookEventDto
            GitHubWebhookEventDto? parsedEvent = null;
            string repositoryName = "unknown";

            try
            {
                parsedEvent = JsonSerializer.Deserialize<GitHubWebhookEventDto>(payload);
                repositoryName = parsedEvent?.Repository?.FullName ?? "unknown";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse GitHub webhook payload as DTO, storing as raw JSON");
                // Continue processing even if parsing fails - we'll store the raw payload
            }

            // Create webhook event entity
            var webhookEvent = new GitHubWebhookEvent
            {
                Id = Guid.NewGuid(),
                TenantId = tenantGuid,
                EventType = eventType,
                RepositoryName = repositoryName,
                Payload = payload,
                Signature = string.Empty, // Signature already verified by controller
                ReceivedAt = DateTime.UtcNow,
                Processed = false
            };

            // Store in database
            _logger.LogInformation("Storing GitHub webhook event in database - EventType: {EventType}", eventType);
            var storedEvent = await _repository.CreateAsync(webhookEvent);

            // Publish to Kafka
            _logger.LogInformation("Publishing GitHub webhook event to Kafka - EventType: {EventType}", eventType);
            await _eventPublisher.PublishWebhookReceivedAsync(storedEvent, parsedEvent, deliveryId);

            // Mark as processed
            _logger.LogInformation("Marking GitHub webhook event as processed - Id: {Id}", storedEvent.Id);
            await _repository.MarkAsProcessedAsync(storedEvent.Id);

            _logger.LogInformation(
                "Successfully processed GitHub webhook - EventType: {EventType}, DeliveryId: {DeliveryId}, EventId: {EventId}",
                eventType, deliveryId, storedEvent.Id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to process GitHub webhook - EventType: {EventType}, DeliveryId: {DeliveryId}",
                eventType, deliveryId);
            return false;
        }
    }

    /// <summary>
    /// Constant-time string comparison to prevent timing attacks
    /// </summary>
    private bool ConstantTimeEquals(string a, string b)
    {
        if (a == null || b == null)
            return false;

        if (a.Length != b.Length)
            return false;

        var result = 0;
        for (var i = 0; i < a.Length; i++)
        {
            result |= a[i] ^ b[i];
        }

        return result == 0;
    }
}

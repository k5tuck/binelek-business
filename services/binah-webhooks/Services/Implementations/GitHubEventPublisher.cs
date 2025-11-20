using Binah.Infrastructure.Kafka;
using Binah.Webhooks.Models.Domain;
using Binah.Webhooks.Models.DTOs.GitHub;
using Binah.Webhooks.Models.Events;
using Binah.Webhooks.Services.Interfaces;
using Binah.Webhooks.Topics;

namespace Binah.Webhooks.Services.Implementations;

/// <summary>
/// Service for publishing GitHub webhook events to Kafka
/// Uses shared KafkaProducer from binah-infrastructure
/// </summary>
public class GitHubEventPublisher : IGitHubEventPublisher
{
    private readonly KafkaProducer _kafkaProducer;
    private readonly ILogger<GitHubEventPublisher> _logger;

    public GitHubEventPublisher(
        KafkaProducer kafkaProducer,
        ILogger<GitHubEventPublisher> logger)
    {
        _kafkaProducer = kafkaProducer ?? throw new ArgumentNullException(nameof(kafkaProducer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Publish a GitHub webhook received event to Kafka
    /// Creates a standardized event following Binah platform conventions
    /// </summary>
    public async Task PublishWebhookReceivedAsync(
        GitHubWebhookEvent webhookEvent,
        GitHubWebhookEventDto? parsedEvent = null,
        string? deliveryId = null)
    {
        try
        {
            var kafkaEvent = new GitHubEventReceivedEvent
            {
                EventId = Guid.NewGuid().ToString(),
                EventType = KafkaTopics.GitHubEventReceived,
                Timestamp = DateTime.UtcNow,
                TenantId = webhookEvent.TenantId.ToString(),
                CorrelationId = deliveryId ?? Guid.NewGuid().ToString(),
                Version = "1.0.0",
                Payload = new GitHubEventPayload
                {
                    EventId = webhookEvent.Id,
                    GitHubEventType = webhookEvent.EventType,
                    RepositoryName = webhookEvent.RepositoryName,
                    DeliveryId = deliveryId ?? string.Empty,
                    RawPayload = webhookEvent.Payload,
                    ParsedEvent = parsedEvent,
                    ReceivedAt = webhookEvent.ReceivedAt,
                    Signature = webhookEvent.Signature
                }
            };

            _logger.LogInformation(
                "Publishing GitHub event to Kafka - EventType: {EventType}, Repository: {Repository}, TenantId: {TenantId}",
                webhookEvent.EventType,
                webhookEvent.RepositoryName,
                webhookEvent.TenantId);

            // Publish to Kafka using the event ID as the key for partitioning
            await _kafkaProducer.ProduceAsync(
                KafkaTopics.GitHubEventReceived,
                kafkaEvent,
                key: kafkaEvent.EventId);

            _logger.LogInformation(
                "Successfully published GitHub event to Kafka - EventId: {EventId}, Topic: {Topic}",
                kafkaEvent.EventId,
                KafkaTopics.GitHubEventReceived);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish GitHub event to Kafka - EventType: {EventType}, Repository: {Repository}",
                webhookEvent.EventType,
                webhookEvent.RepositoryName);
            throw;
        }
    }
}

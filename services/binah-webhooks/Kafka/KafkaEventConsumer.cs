using Confluent.Kafka;
using Binah.Webhooks.Services;
using System.Text.Json;

namespace Binah.Webhooks.Kafka;

/// <summary>
/// Kafka consumer for domain events that trigger webhooks
/// Listens to all events and dispatches to registered webhook subscriptions
/// </summary>
public class KafkaEventConsumer : BackgroundService
{
    private readonly ILogger<KafkaEventConsumer> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly List<string> _eventTopics;

    public KafkaEventConsumer(
        ILogger<KafkaEventConsumer> logger,
        IConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        // Subscribe to all relevant event topics
        _eventTopics = new List<string>
        {
            "user-events",
            "property-events",
            "entity-events",
            "ontology-events",
            "pipeline-events",
            "subscription-events"
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Kafka Event Consumer starting...");

        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = "webhook-service",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(_eventTopics);

        _logger.LogInformation("Subscribed to topics: {Topics}", string.Join(", ", _eventTopics));

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Use timeout-based consume to prevent thread pool blocking
                    var consumeResult = consumer.Consume(TimeSpan.FromSeconds(1));

                    if (consumeResult?.Message != null)
                    {
                        await ProcessEventAsync(consumeResult.Message, stoppingToken);

                        // Commit offset after successful processing
                        consumer.Commit(consumeResult);
                    }
                    else
                    {
                        // No message received, yield to prevent tight loop
                        await Task.Delay(100, stoppingToken);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming Kafka message");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing event");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Kafka Event Consumer shutting down...");
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task ProcessEventAsync(Message<string, string> message, CancellationToken cancellationToken)
    {
        try
        {
            // Parse event
            var eventData = JsonSerializer.Deserialize<DomainEvent>(message.Value);

            if (eventData == null)
            {
                _logger.LogWarning("Failed to deserialize event: {Message}", message.Value);
                return;
            }

            _logger.LogInformation("Processing event: {EventType} for tenant {TenantId}",
                eventData.EventType, eventData.TenantId);

            // Use a new scope for each event to get scoped services
            using var scope = _serviceProvider.CreateScope();
            var deliveryService = scope.ServiceProvider.GetRequiredService<IWebhookDeliveryService>();

            // Trigger webhook delivery
            await deliveryService.DeliverWebhookAsync(
                eventData.EventType,
                eventData.Payload,
                eventData.TenantId);

            _logger.LogInformation("Successfully processed event: {EventType}", eventData.EventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing event from Kafka");
            throw;
        }
    }
}

/// <summary>
/// Domain event model from Kafka
/// </summary>
public class DomainEvent
{
    public string EventType { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public object Payload { get; set; } = new();
    public DateTime Timestamp { get; set; }
}

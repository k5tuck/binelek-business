namespace Binah.Webhooks.Topics;

/// <summary>
/// Kafka topic names for the webhooks service
/// </summary>
public static class KafkaTopics
{
    /// <summary>
    /// Topic for GitHub webhook events received from GitHub
    /// Published when a webhook is received, verified, and stored
    /// </summary>
    public const string GitHubEventReceived = "github.event.received.v1";

    /// <summary>
    /// Topic for webhook delivery status updates
    /// Published when a webhook is delivered to a subscriber
    /// </summary>
    public const string WebhookDelivered = "webhook.delivered.v1";

    /// <summary>
    /// Topic for webhook delivery failures
    /// Published when webhook delivery fails after all retries
    /// </summary>
    public const string WebhookDeliveryFailed = "webhook.delivery.failed.v1";

    /// <summary>
    /// Topic for autonomous pull request created events
    /// Published when an autonomous PR is successfully created
    /// </summary>
    public const string AutonomousPRCreated = "autonomous.pr.created.v1";

    /// <summary>
    /// Topic for autonomous pull request merged events
    /// Published when an autonomous PR is successfully merged
    /// </summary>
    public const string AutonomousPRMerged = "autonomous.pr.merged.v1";

    /// <summary>
    /// Topic for autonomous pull request failed events
    /// Published when autonomous PR creation fails
    /// </summary>
    public const string AutonomousPRFailed = "autonomous.pr.failed.v1";
}

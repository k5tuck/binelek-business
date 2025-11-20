namespace Binah.Webhooks.Services.Interfaces;

/// <summary>
/// Interface for GitHub webhook service
/// </summary>
public interface IGitHubWebhookService
{
    /// <summary>
    /// Verify GitHub webhook signature using HMAC-SHA256
    /// </summary>
    /// <param name="payload">Raw request payload</param>
    /// <param name="signature">X-Hub-Signature-256 header value</param>
    /// <param name="secret">GitHub webhook secret</param>
    /// <returns>True if signature is valid, false otherwise</returns>
    bool VerifyGitHubSignature(string payload, string signature, string secret);

    /// <summary>
    /// Process GitHub webhook event
    /// </summary>
    /// <param name="eventType">GitHub event type (X-GitHub-Event header)</param>
    /// <param name="deliveryId">GitHub delivery ID (X-GitHub-Delivery header)</param>
    /// <param name="payload">Raw JSON payload</param>
    /// <param name="tenantId">Tenant ID</param>
    /// <returns>Processing result</returns>
    Task<bool> ProcessWebhookAsync(string eventType, string deliveryId, string payload, string tenantId);
}

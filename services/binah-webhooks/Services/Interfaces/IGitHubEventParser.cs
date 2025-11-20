using Binah.Webhooks.Models.DTOs.GitHub;

namespace Binah.Webhooks.Services.Interfaces;

/// <summary>
/// Service for parsing GitHub webhook event payloads into strongly-typed DTOs
/// </summary>
public interface IGitHubEventParser
{
    /// <summary>
    /// Parse a GitHub webhook event payload into a strongly-typed DTO
    /// </summary>
    /// <typeparam name="T">Expected DTO type (must inherit from GitHubWebhookEventDto)</typeparam>
    /// <param name="eventType">GitHub event type (from X-GitHub-Event header)</param>
    /// <param name="payload">JSON payload from GitHub webhook</param>
    /// <returns>Parsed DTO of type T</returns>
    /// <exception cref="ArgumentNullException">Thrown when eventType or payload is null/empty</exception>
    /// <exception cref="ArgumentException">Thrown when eventType doesn't match expected type T</exception>
    /// <exception cref="System.Text.Json.JsonException">Thrown when payload is invalid JSON</exception>
    /// <exception cref="InvalidOperationException">Thrown when required fields are missing from payload</exception>
    T ParseEvent<T>(string eventType, string payload) where T : GitHubWebhookEventDto;

    /// <summary>
    /// Parse a GitHub webhook event payload into a base DTO without type safety
    /// Useful when you don't know the event type ahead of time
    /// </summary>
    /// <param name="eventType">GitHub event type (from X-GitHub-Event header)</param>
    /// <param name="payload">JSON payload from GitHub webhook</param>
    /// <returns>Parsed DTO (type depends on eventType)</returns>
    /// <exception cref="ArgumentNullException">Thrown when eventType or payload is null/empty</exception>
    /// <exception cref="NotSupportedException">Thrown when eventType is not supported</exception>
    /// <exception cref="System.Text.Json.JsonException">Thrown when payload is invalid JSON</exception>
    GitHubWebhookEventDto ParseEvent(string eventType, string payload);

    /// <summary>
    /// Validate that a GitHub event payload contains all required fields
    /// </summary>
    /// <param name="eventType">GitHub event type</param>
    /// <param name="payload">JSON payload</param>
    /// <returns>True if valid, false otherwise</returns>
    bool ValidateEventPayload(string eventType, string payload);

    /// <summary>
    /// Get the list of supported GitHub event types
    /// </summary>
    /// <returns>Array of supported event type names</returns>
    string[] GetSupportedEventTypes();
}

using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Binah.Webhooks.Integrations;

/// <summary>
/// Slack Integration for sending notifications to Slack channels
/// </summary>
public interface ISlackIntegration
{
    Task<bool> SendNotificationAsync(string webhookUrl, SlackMessage message);
    Task<bool> SendSimpleMessageAsync(string webhookUrl, string text);
    SlackMessage CreateEntityNotification(string entityType, string action, Dictionary<string, object> entityData);
}

public class SlackIntegration : ISlackIntegration
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SlackIntegration> _logger;

    public SlackIntegration(HttpClient httpClient, ILogger<SlackIntegration> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Send a formatted Slack message
    /// </summary>
    public async Task<bool> SendNotificationAsync(string webhookUrl, SlackMessage message)
    {
        try
        {
            var payload = new
            {
                text = message.Text,
                username = message.Username ?? "Binelek Bot",
                icon_emoji = message.IconEmoji ?? ":robot_face:",
                channel = message.Channel,
                attachments = message.Attachments?.Select(a => new
                {
                    color = a.Color,
                    title = a.Title,
                    text = a.Text,
                    fields = a.Fields?.Select(f => new
                    {
                        title = f.Title,
                        value = f.Value,
                        @short = f.Short
                    }),
                    footer = a.Footer,
                    ts = a.Timestamp != null ? ((DateTimeOffset)a.Timestamp).ToUnixTimeSeconds() : (long?)null
                })
            };

            var response = await _httpClient.PostAsJsonAsync(webhookUrl, payload);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Slack notification sent successfully");
                return true;
            }

            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to send Slack notification. Status: {Status}, Error: {Error}",
                response.StatusCode, error);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while sending Slack notification");
            return false;
        }
    }

    /// <summary>
    /// Send a simple text message to Slack
    /// </summary>
    public async Task<bool> SendSimpleMessageAsync(string webhookUrl, string text)
    {
        var message = new SlackMessage
        {
            Text = text
        };

        return await SendNotificationAsync(webhookUrl, message);
    }

    /// <summary>
    /// Create a formatted notification for entity events (create, update, delete)
    /// </summary>
    public SlackMessage CreateEntityNotification(
        string entityType,
        string action,
        Dictionary<string, object> entityData)
    {
        var color = action.ToLower() switch
        {
            "created" => "#36a64f",  // Green
            "updated" => "#2196F3",  // Blue
            "deleted" => "#ff0000",  // Red
            _ => "#808080"           // Gray
        };

        var emoji = action.ToLower() switch
        {
            "created" => ":white_check_mark:",
            "updated" => ":pencil2:",
            "deleted" => ":wastebasket:",
            _ => ":information_source:"
        };

        var fields = new List<SlackField>();

        // Add key entity attributes as fields
        foreach (var (key, value) in entityData.Take(5))
        {
            fields.Add(new SlackField
            {
                Title = FormatFieldName(key),
                Value = value?.ToString() ?? "-",
                Short = true
            });
        }

        var attachment = new SlackAttachment
        {
            Color = color,
            Title = $"{entityType} {action}",
            Text = $"A {entityType.ToLower()} was {action.ToLower()}",
            Fields = fields,
            Footer = "Binelek Platform",
            Timestamp = DateTime.UtcNow
        };

        return new SlackMessage
        {
            Text = $"{emoji} *{entityType} {action}*",
            Username = "Binelek Bot",
            IconEmoji = ":robot_face:",
            Attachments = new List<SlackAttachment> { attachment }
        };
    }

    private string FormatFieldName(string fieldName)
    {
        // Convert snake_case or camelCase to Title Case
        return string.Concat(fieldName
            .Select((c, i) =>
                i == 0 ? char.ToUpper(c).ToString() :
                c == '_' ? " " :
                char.IsUpper(c) && i > 0 ? $" {c}" : c.ToString()));
    }
}

/// <summary>
/// Slack message model
/// </summary>
public class SlackMessage
{
    public string Text { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? IconEmoji { get; set; }
    public string? Channel { get; set; }
    public List<SlackAttachment>? Attachments { get; set; }
}

/// <summary>
/// Slack attachment (rich formatting)
/// </summary>
public class SlackAttachment
{
    public string? Color { get; set; }
    public string? Title { get; set; }
    public string? Text { get; set; }
    public List<SlackField>? Fields { get; set; }
    public string? Footer { get; set; }
    public DateTime? Timestamp { get; set; }
}

/// <summary>
/// Slack field (key-value pair in attachment)
/// </summary>
public class SlackField
{
    public string Title { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool Short { get; set; } = true;  // Display side-by-side
}

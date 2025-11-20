// NOTE: This webhook implementation is currently part of binah-auth for expedient delivery.
// FUTURE: Extract to a separate binah-webhooks microservice with its own database and event bus integration.
// The service should be horizontally scalable and handle webhook delivery asynchronously via a queue.

using Binah.Webhooks.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Binah.Webhooks.Services;

/// <summary>
/// Webhook management service
/// TODO: Extract to separate binah-webhooks microservice
/// </summary>
public class WebhookService : IWebhookService
{
    private readonly WebhookDbContext _context;
    private readonly ILogger<WebhookService> _logger;
    private readonly ITenantContext _tenantContext;
    private readonly UrlValidator _urlValidator;

    public WebhookService(
        WebhookDbContext context,
        ILogger<WebhookService> logger,
        ITenantContext tenantContext,
        UrlValidator urlValidator)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _urlValidator = urlValidator ?? throw new ArgumentNullException(nameof(urlValidator));
    }

    public async Task<WebhookSubscription> CreateSubscriptionAsync(string tenantId, WebhookSubscriptionRequest request)
    {
        // Validate tenant ID from context matches the provided tenantId
        if (_tenantContext.TenantId != tenantId)
        {
            _logger.LogWarning("Tenant ID mismatch: context={ContextTenantId}, provided={ProvidedTenantId}",
                _tenantContext.TenantId, tenantId);
            throw new UnauthorizedAccessException("Tenant ID mismatch");
        }

        // SSRF protection - validate URL before saving
        if (!_urlValidator.IsUrlSafe(request.Url))
        {
            var error = _urlValidator.GetValidationError(request.Url);
            _logger.LogWarning("SSRF protection blocked webhook URL: {Url}, Reason: {Reason}", request.Url, error);
            throw new ArgumentException($"Invalid webhook URL: {error}");
        }

        var subscription = new WebhookSubscription
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = request.Name,
            Url = request.Url,
            Events = JsonSerializer.Serialize(request.Events),
            Secret = GenerateSecret(),
            Active = request.Active,
            Headers = JsonSerializer.Serialize(request.Headers),
            RetryCount = Math.Min(request.RetryCount, 5), // Max 5 retries
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.WebhookSubscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Webhook subscription created for tenant {TenantId}: {Name}", tenantId, request.Name);

        return subscription;
    }

    public async Task<List<WebhookSubscription>> GetSubscriptionsAsync(string tenantId)
    {
        return await _context.WebhookSubscriptions
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<WebhookSubscription?> GetSubscriptionAsync(Guid id, string tenantId)
    {
        return await _context.WebhookSubscriptions
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId);
    }

    public async Task<WebhookSubscription> UpdateSubscriptionAsync(Guid id, string tenantId, WebhookSubscriptionRequest request)
    {
        // Validate tenant ID from context matches the provided tenantId
        if (_tenantContext.TenantId != tenantId)
        {
            _logger.LogWarning("Tenant ID mismatch: context={ContextTenantId}, provided={ProvidedTenantId}",
                _tenantContext.TenantId, tenantId);
            throw new UnauthorizedAccessException("Tenant ID mismatch");
        }

        var subscription = await GetSubscriptionAsync(id, tenantId);

        if (subscription == null)
        {
            throw new InvalidOperationException($"Webhook subscription {id} not found");
        }

        // SSRF protection - validate URL before updating
        if (!_urlValidator.IsUrlSafe(request.Url))
        {
            var error = _urlValidator.GetValidationError(request.Url);
            _logger.LogWarning("SSRF protection blocked webhook URL: {Url}, Reason: {Reason}", request.Url, error);
            throw new ArgumentException($"Invalid webhook URL: {error}");
        }

        subscription.Name = request.Name;
        subscription.Url = request.Url;
        subscription.Events = JsonSerializer.Serialize(request.Events);
        subscription.Active = request.Active;
        subscription.Headers = JsonSerializer.Serialize(request.Headers);
        subscription.RetryCount = Math.Min(request.RetryCount, 5);
        subscription.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Webhook subscription updated: {Id}", id);

        return subscription;
    }

    public async Task DeleteSubscriptionAsync(Guid id, string tenantId)
    {
        var subscription = await GetSubscriptionAsync(id, tenantId);

        if (subscription != null)
        {
            _context.WebhookSubscriptions.Remove(subscription);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Webhook subscription deleted: {Id}", id);
        }
    }

    public async Task<bool> TestWebhookAsync(Guid id, string tenantId)
    {
        var subscription = await GetSubscriptionAsync(id, tenantId);

        if (subscription == null)
        {
            return false;
        }

        var testPayload = new
        {
            @event = "webhook.test",
            timestamp = DateTime.UtcNow,
            data = new { message = "This is a test webhook" }
        };

        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var payloadJson = JsonSerializer.Serialize(testPayload);
            var signature = GenerateSignatureFromService(payloadJson, subscription.Secret);

            var request = new HttpRequestMessage(HttpMethod.Post, subscription.Url)
            {
                Content = new StringContent(payloadJson, System.Text.Encoding.UTF8, "application/json")
            };

            request.Headers.Add("X-Webhook-Signature", signature);
            request.Headers.Add("X-Webhook-Event", "webhook.test");

            var response = await httpClient.SendAsync(request);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test webhook {Id}", id);
            return false;
        }
    }

    public async Task<List<WebhookDelivery>> GetDeliveriesAsync(Guid subscriptionId, string tenantId, int skip = 0, int take = 50)
    {
        // Verify subscription belongs to tenant
        var subscription = await GetSubscriptionAsync(subscriptionId, tenantId);

        if (subscription == null)
        {
            return new List<WebhookDelivery>();
        }

        return await _context.WebhookDeliveries
            .Where(d => d.SubscriptionId == subscriptionId)
            .OrderByDescending(d => d.DeliveredAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<List<string>> GetAvailableEventsAsync()
    {
        // Return list of available webhook events
        return new List<string>
        {
            "user.created",
            "user.updated",
            "user.deleted",
            "user.login",
            "user.logout",
            "property.created",
            "property.updated",
            "property.deleted",
            "entity.created",
            "entity.updated",
            "entity.deleted",
            "ontology.published",
            "pipeline.executed",
            "subscription.created",
            "subscription.updated",
            "subscription.cancelled"
        };
    }


    private string GenerateSecret()
    {
        return Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
    }

    private string GenerateSignatureFromService(string payload, string secret)
    {
        using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLower();
    }
}

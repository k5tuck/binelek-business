using Binah.Webhooks.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Binah.Webhooks.HealthChecks;

/// <summary>
/// Health check for monitoring webhook delivery queue size
/// </summary>
public class WebhookQueueHealthCheck : IHealthCheck
{
    private readonly WebhookDbContext _dbContext;
    private readonly ILogger<WebhookQueueHealthCheck> _logger;

    public WebhookQueueHealthCheck(
        WebhookDbContext dbContext,
        ILogger<WebhookQueueHealthCheck> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var pendingCount = await _dbContext.WebhookDeliveries
                .Where(w => w.ResponseStatus == "pending")
                .CountAsync(cancellationToken);

            if (pendingCount > 1000)
            {
                _logger.LogWarning("High pending webhook count: {Count}", pendingCount);
                return HealthCheckResult.Degraded(
                    $"High pending webhook count: {pendingCount} deliveries",
                    data: new Dictionary<string, object>
                    {
                        { "pendingCount", pendingCount }
                    });
            }

            return HealthCheckResult.Healthy(
                $"Pending webhooks: {pendingCount}",
                data: new Dictionary<string, object>
                {
                    { "pendingCount", pendingCount }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check webhook delivery queue health");
            return HealthCheckResult.Unhealthy(
                "Failed to check webhook delivery queue",
                ex);
        }
    }
}

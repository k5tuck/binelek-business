using Binah.Webhooks.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Binah.Webhooks.HealthChecks;

/// <summary>
/// Health check for monitoring failed webhook retry queue
/// </summary>
public class RetryQueueHealthCheck : IHealthCheck
{
    private readonly WebhookDbContext _dbContext;
    private readonly ILogger<RetryQueueHealthCheck> _logger;

    public RetryQueueHealthCheck(
        WebhookDbContext dbContext,
        ILogger<RetryQueueHealthCheck> logger)
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
            var failedCount = await _dbContext.WebhookDeliveries
                .Where(w => w.ResponseStatus == "failed" && w.AttemptNumber >= 3)
                .CountAsync(cancellationToken);

            if (failedCount > 100)
            {
                _logger.LogWarning("High failed webhook count: {Count}", failedCount);
                return HealthCheckResult.Degraded(
                    $"High failed webhook count: {failedCount} deliveries",
                    data: new Dictionary<string, object>
                    {
                        { "failedCount", failedCount }
                    });
            }

            return HealthCheckResult.Healthy(
                $"Failed webhooks: {failedCount}",
                data: new Dictionary<string, object>
                {
                    { "failedCount", failedCount }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check retry queue health");
            return HealthCheckResult.Unhealthy(
                "Failed to check retry queue",
                ex);
        }
    }
}

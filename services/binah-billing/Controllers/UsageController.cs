using Binah.Billing.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Binah.Billing.Controllers;

[ApiController]
[Route("api/usage")]
public class UsageController : ControllerBase
{
    private readonly BillingDbContext _dbContext;
    private readonly ILogger<UsageController> _logger;

    public UsageController(BillingDbContext dbContext, ILogger<UsageController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Get usage summary for the current tenant
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult<UsageSummaryResponse>> GetSummary()
    {
        // Extract tenant_id from JWT (source of truth)
        var tenantIdStr = User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(tenantIdStr))
        {
            return Unauthorized(new { error = "Tenant ID not found in token" });
        }

        if (!Guid.TryParse(tenantIdStr, out var tenantId))
        {
            return BadRequest(new { error = "Invalid tenant ID format" });
        }

        _logger.LogInformation("Getting usage summary for tenant {TenantId}", tenantId);

        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        // Get API calls from usage records
        var apiCallsToday = await _dbContext.UsageRecords
            .Where(r => r.TenantId == tenantId && r.UsageType == "api_calls" && r.Timestamp >= today)
            .SumAsync(r => r.Quantity);

        var apiCallsThisMonth = await _dbContext.UsageRecords
            .Where(r => r.TenantId == tenantId && r.UsageType == "api_calls" && r.Timestamp >= monthStart)
            .SumAsync(r => r.Quantity);

        // Get active users
        var activeUsersToday = await _dbContext.UsageRecords
            .Where(r => r.TenantId == tenantId && r.UsageType == "active_users" && r.Timestamp >= today)
            .Select(r => r.Quantity)
            .FirstOrDefaultAsync();

        var activeUsersThisMonth = await _dbContext.UsageRecords
            .Where(r => r.TenantId == tenantId && r.UsageType == "active_users" && r.Timestamp >= monthStart)
            .MaxAsync(r => (int?)r.Quantity) ?? 0;

        // Get storage usage
        var storageUsed = await _dbContext.UsageRecords
            .Where(r => r.TenantId == tenantId && r.UsageType == "storage_gb")
            .OrderByDescending(r => r.Timestamp)
            .Select(r => r.Quantity)
            .FirstOrDefaultAsync();

        // Get entities count
        var totalEntities = await _dbContext.UsageRecords
            .Where(r => r.TenantId == tenantId && r.UsageType == "entities")
            .OrderByDescending(r => r.Timestamp)
            .Select(r => r.Quantity)
            .FirstOrDefaultAsync();

        var entitiesCreatedThisMonth = await _dbContext.UsageRecords
            .Where(r => r.TenantId == tenantId && r.UsageType == "entities_created" && r.Timestamp >= monthStart)
            .SumAsync(r => r.Quantity);

        // Get limits from subscription
        var subscription = await _dbContext.Subscriptions
            .Where(s => s.Customer.TenantId == tenantId.ToString() && s.Status == "active")
            .Include(s => s.Customer)
            .FirstOrDefaultAsync();

        var monthlyLimit = 1000000L; // Default
        var storageLimit = 1000.0; // Default
        var entityLimit = 1000000L; // Default

        var response = new UsageSummaryResponse
        {
            ApiCalls = new ApiCallsUsage
            {
                Today = apiCallsToday,
                ThisMonth = apiCallsThisMonth,
                MonthlyLimit = monthlyLimit
            },
            ActiveUsers = new ActiveUsersUsage
            {
                Today = activeUsersToday,
                ThisMonth = activeUsersThisMonth,
                TotalRegistered = 0 // Would come from auth service
            },
            Storage = new StorageUsage
            {
                UsedGb = storageUsed / 1024.0, // Convert MB to GB
                LimitGb = storageLimit
            },
            Entities = new EntitiesUsage
            {
                Total = totalEntities,
                CreatedThisMonth = entitiesCreatedThisMonth,
                Limit = entityLimit
            }
        };

        return Ok(response);
    }

    /// <summary>
    /// Get usage breakdown by feature
    /// </summary>
    [HttpGet("by-feature")]
    public async Task<ActionResult<List<FeatureUsage>>> GetUsageByFeature()
    {
        // Extract tenant_id from JWT (source of truth)
        var tenantIdStr = User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(tenantIdStr))
        {
            return Unauthorized(new { error = "Tenant ID not found in token" });
        }

        if (!Guid.TryParse(tenantIdStr, out var tenantId))
        {
            return BadRequest(new { error = "Invalid tenant ID format" });
        }

        _logger.LogInformation("Getting feature usage for tenant {TenantId}", tenantId);

        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        // Get usage by feature from usage records
        var featureUsage = await _dbContext.UsageRecords
            .Where(r => r.TenantId == tenantId && r.UsageType.StartsWith("feature_") && r.Timestamp >= monthStart)
            .GroupBy(r => r.UsageType)
            .Select(g => new
            {
                Feature = g.Key.Replace("feature_", ""),
                ApiCalls = g.Sum(r => r.Quantity)
            })
            .ToListAsync();

        var totalCalls = featureUsage.Sum(f => f.ApiCalls);

        var features = new List<FeatureUsage>
        {
            new() { Feature = "Entity Management", ApiCalls = featureUsage.FirstOrDefault(f => f.Feature == "entity_management")?.ApiCalls ?? 0 },
            new() { Feature = "Graph Queries", ApiCalls = featureUsage.FirstOrDefault(f => f.Feature == "graph_queries")?.ApiCalls ?? 0 },
            new() { Feature = "Search", ApiCalls = featureUsage.FirstOrDefault(f => f.Feature == "search")?.ApiCalls ?? 0 },
            new() { Feature = "AI Platform", ApiCalls = featureUsage.FirstOrDefault(f => f.Feature == "ai_platform")?.ApiCalls ?? 0 },
            new() { Feature = "Pipelines", ApiCalls = featureUsage.FirstOrDefault(f => f.Feature == "pipelines")?.ApiCalls ?? 0 }
        };

        // Calculate percentages
        foreach (var feature in features)
        {
            feature.Percentage = totalCalls > 0 ? (double)feature.ApiCalls / totalCalls * 100 : 0;
        }

        return Ok(features);
    }

    /// <summary>
    /// Get most active users for the current tenant
    /// </summary>
    [HttpGet("top-users")]
    public async Task<ActionResult<List<UserUsage>>> GetTopUsers([FromQuery] int limit = 10)
    {
        // Extract tenant_id from JWT (source of truth)
        var tenantIdStr = User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(tenantIdStr))
        {
            return Unauthorized(new { error = "Tenant ID not found in token" });
        }

        if (!Guid.TryParse(tenantIdStr, out var tenantId))
        {
            return BadRequest(new { error = "Invalid tenant ID format" });
        }

        _logger.LogInformation("Getting top {Limit} users for tenant {TenantId}", limit, tenantId);

        // TODO: In production, query from auth service with API call counts
        var users = new List<UserUsage>();

        return Ok(users);
    }

    /// <summary>
    /// Get historical usage data for charts
    /// </summary>
    [HttpGet("history")]
    public async Task<ActionResult<UsageHistoryResponse>> GetHistory([FromQuery] int days = 30)
    {
        // Extract tenant_id from JWT (source of truth)
        var tenantIdStr = User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(tenantIdStr))
        {
            return Unauthorized(new { error = "Tenant ID not found in token" });
        }

        if (!Guid.TryParse(tenantIdStr, out var tenantId))
        {
            return BadRequest(new { error = "Invalid tenant ID format" });
        }

        _logger.LogInformation("Getting {Days} days of usage history for tenant {TenantId}", days, tenantId);

        var startDate = DateTime.UtcNow.Date.AddDays(-(days - 1));

        // Get daily aggregated usage
        var dailyApiCalls = await _dbContext.UsageRecords
            .Where(r => r.TenantId == tenantId && r.UsageType == "api_calls" && r.Timestamp >= startDate)
            .GroupBy(r => r.Timestamp.Date)
            .Select(g => new { Date = g.Key, Total = g.Sum(r => r.Quantity) })
            .ToDictionaryAsync(g => g.Date, g => g.Total);

        var dailyActiveUsers = await _dbContext.UsageRecords
            .Where(r => r.TenantId == tenantId && r.UsageType == "active_users" && r.Timestamp >= startDate)
            .GroupBy(r => r.Timestamp.Date)
            .Select(g => new { Date = g.Key, Max = g.Max(r => r.Quantity) })
            .ToDictionaryAsync(g => g.Date, g => g.Max);

        var dataPoints = new List<DailyUsage>();

        for (int i = days - 1; i >= 0; i--)
        {
            var date = DateTime.UtcNow.Date.AddDays(-i);
            dataPoints.Add(new DailyUsage
            {
                Date = date.ToString("yyyy-MM-dd"),
                ApiCalls = dailyApiCalls.TryGetValue(date, out var calls) ? calls : 0,
                ActiveUsers = dailyActiveUsers.TryGetValue(date, out var users) ? users : 0
            });
        }

        return Ok(new UsageHistoryResponse { DataPoints = dataPoints });
    }
}

public class UsageSummaryResponse
{
    public ApiCallsUsage ApiCalls { get; set; } = new();
    public ActiveUsersUsage ActiveUsers { get; set; } = new();
    public StorageUsage Storage { get; set; } = new();
    public EntitiesUsage Entities { get; set; } = new();
}

public class ApiCallsUsage
{
    public long Today { get; set; }
    public long ThisMonth { get; set; }
    public long MonthlyLimit { get; set; }
}

public class ActiveUsersUsage
{
    public int Today { get; set; }
    public int ThisMonth { get; set; }
    public int TotalRegistered { get; set; }
}

public class StorageUsage
{
    public double UsedGb { get; set; }
    public double LimitGb { get; set; }
}

public class EntitiesUsage
{
    public long Total { get; set; }
    public long CreatedThisMonth { get; set; }
    public long Limit { get; set; }
}

public class FeatureUsage
{
    public string Feature { get; set; } = string.Empty;
    public long ApiCalls { get; set; }
    public double Percentage { get; set; }
}

public class UserUsage
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public long ApiCalls { get; set; }
}

public class UsageHistoryResponse
{
    public List<DailyUsage> DataPoints { get; set; } = new();
}

public class DailyUsage
{
    public string Date { get; set; } = string.Empty;
    public long ApiCalls { get; set; }
    public int ActiveUsers { get; set; }
}

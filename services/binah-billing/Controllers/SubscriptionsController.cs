using Binah.Billing.Models.DTOs;
using Binah.Billing.Services;
using Binah.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Binah.Billing.Controllers;

[ApiController]
[Route("api/billing/subscriptions")]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<SubscriptionsController> _logger;

    public SubscriptionsController(
        ISubscriptionService subscriptionService,
        ILogger<SubscriptionsController> logger)
    {
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<SubscriptionResponse>>> CreateSubscription(
        [FromBody] CreateSubscriptionRequest request)
    {
        // Extract tenant_id from JWT (source of truth)
        var jwtTenantId = User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(jwtTenantId))
        {
            return Unauthorized(ApiResponse<SubscriptionResponse>.WithError("Tenant ID not found in token"));
        }

        // Validate client tenant matches JWT tenant
        if (request.TenantId.ToString() != jwtTenantId)
        {
            _logger.LogWarning("Tenant ID mismatch: JWT={JwtTenantId}, Request={RequestTenantId}",
                jwtTenantId, request.TenantId);
            return StatusCode(403, ApiResponse<SubscriptionResponse>.WithError("Tenant ID in request does not match authenticated tenant"));
        }

        try
        {
            var subscription = await _subscriptionService.CreateSubscriptionAsync(request);
            return Ok(ApiResponse<SubscriptionResponse>.Ok(subscription));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid subscription request");
            return BadRequest(ApiResponse<SubscriptionResponse>.WithError(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create subscription");
            return StatusCode(500, ApiResponse<SubscriptionResponse>.WithError("Failed to create subscription"));
        }
    }

    [HttpGet("{subscriptionId}")]
    public async Task<ActionResult<ApiResponse<SubscriptionResponse>>> GetSubscription(Guid subscriptionId)
    {
        // Extract tenant_id from JWT (source of truth)
        var jwtTenantId = User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(jwtTenantId))
        {
            return Unauthorized(ApiResponse<SubscriptionResponse>.WithError("Tenant ID not found in token"));
        }

        try
        {
            var subscription = await _subscriptionService.GetSubscriptionAsync(subscriptionId);

            // Validate subscription belongs to authenticated tenant
            if (subscription.CustomerId.ToString() != jwtTenantId)
            {
                _logger.LogWarning("Tenant {TenantId} attempted to access subscription {SubscriptionId} belonging to different tenant",
                    jwtTenantId, subscriptionId);
                return StatusCode(403, ApiResponse<SubscriptionResponse>.WithError("Cannot access other tenant's subscription data"));
            }

            return Ok(ApiResponse<SubscriptionResponse>.Ok(subscription));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<SubscriptionResponse>.WithError(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get subscription {SubscriptionId}", subscriptionId);
            return StatusCode(500, ApiResponse<SubscriptionResponse>.WithError("Failed to get subscription"));
        }
    }

    [HttpGet("tenant/{tenantId}/active")]
    public async Task<ActionResult<ApiResponse<SubscriptionResponse>>> GetActiveSubscription(Guid tenantId)
    {
        // Extract tenant_id from JWT (source of truth)
        var jwtTenantId = User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(jwtTenantId))
        {
            return Unauthorized(ApiResponse<SubscriptionResponse>.WithError("Tenant ID not found in token"));
        }

        // CRITICAL: Validate route tenant matches JWT tenant (prevent tenant forgery)
        if (tenantId.ToString() != jwtTenantId)
        {
            _logger.LogWarning("Tenant forgery attempt: JWT={JwtTenantId}, Route={RouteTenantId}",
                jwtTenantId, tenantId);
            return StatusCode(403, ApiResponse<SubscriptionResponse>.WithError("Cannot access other tenant's subscription data"));
        }

        try
        {
            var subscription = await _subscriptionService.GetActiveSubscriptionByTenantAsync(tenantId);
            if (subscription == null)
            {
                return NotFound(ApiResponse<SubscriptionResponse>.WithError("No active subscription found"));
            }
            return Ok(ApiResponse<SubscriptionResponse>.Ok(subscription));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active subscription for tenant {TenantId}", tenantId);
            return StatusCode(500, ApiResponse<SubscriptionResponse>.WithError("Failed to get subscription"));
        }
    }

    [HttpGet("tenant/{tenantId}")]
    public async Task<ActionResult<ApiResponse<List<SubscriptionResponse>>>> GetSubscriptionHistory(Guid tenantId)
    {
        // Extract tenant_id from JWT (source of truth)
        var jwtTenantId = User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(jwtTenantId))
        {
            return Unauthorized(ApiResponse<List<SubscriptionResponse>>.WithError("Tenant ID not found in token"));
        }

        // Validate route tenant matches JWT tenant
        if (tenantId.ToString() != jwtTenantId)
        {
            _logger.LogWarning("Tenant {TenantId} attempted to access subscription history for different tenant {RouteTenantId}",
                jwtTenantId, tenantId);
            return StatusCode(403, ApiResponse<List<SubscriptionResponse>>.WithError("Cannot access other tenant's subscription history"));
        }

        try
        {
            var subscriptions = await _subscriptionService.GetSubscriptionHistoryAsync(tenantId);
            return Ok(ApiResponse<List<SubscriptionResponse>>.Ok(subscriptions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get subscription history for tenant {TenantId}", tenantId);
            return StatusCode(500, ApiResponse<List<SubscriptionResponse>>.WithError("Failed to get subscription history"));
        }
    }

    [HttpPut("{subscriptionId}")]
    public async Task<ActionResult<ApiResponse<SubscriptionResponse>>> UpdateSubscription(
        Guid subscriptionId,
        [FromBody] UpdateSubscriptionRequest request)
    {
        // Extract tenant_id from JWT (source of truth)
        var jwtTenantId = User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(jwtTenantId))
        {
            return Unauthorized(ApiResponse<SubscriptionResponse>.WithError("Tenant ID not found in token"));
        }

        try
        {
            // First, verify subscription belongs to authenticated tenant
            var existingSubscription = await _subscriptionService.GetSubscriptionAsync(subscriptionId);

            if (existingSubscription.CustomerId.ToString() != jwtTenantId)
            {
                _logger.LogWarning("Tenant {TenantId} attempted to update subscription {SubscriptionId} belonging to different tenant",
                    jwtTenantId, subscriptionId);
                return StatusCode(403, ApiResponse<SubscriptionResponse>.WithError("Cannot update other tenant's subscription"));
            }

            var subscription = await _subscriptionService.UpdateSubscriptionAsync(subscriptionId, request);
            return Ok(ApiResponse<SubscriptionResponse>.Ok(subscription));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<SubscriptionResponse>.WithError(ex.Message));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid update request");
            return BadRequest(ApiResponse<SubscriptionResponse>.WithError(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update subscription {SubscriptionId}", subscriptionId);
            return StatusCode(500, ApiResponse<SubscriptionResponse>.WithError("Failed to update subscription"));
        }
    }

    [HttpPost("{subscriptionId}/cancel")]
    public async Task<ActionResult<ApiResponse<SubscriptionResponse>>> CancelSubscription(
        Guid subscriptionId,
        [FromBody] CancelSubscriptionRequest request)
    {
        // Extract tenant_id from JWT (source of truth)
        var jwtTenantId = User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(jwtTenantId))
        {
            return Unauthorized(ApiResponse<SubscriptionResponse>.WithError("Tenant ID not found in token"));
        }

        try
        {
            // First, verify subscription belongs to authenticated tenant
            var existingSubscription = await _subscriptionService.GetSubscriptionAsync(subscriptionId);

            if (existingSubscription.CustomerId.ToString() != jwtTenantId)
            {
                _logger.LogWarning("Tenant {TenantId} attempted to cancel subscription {SubscriptionId} belonging to different tenant",
                    jwtTenantId, subscriptionId);
                return StatusCode(403, ApiResponse<SubscriptionResponse>.WithError("Cannot cancel other tenant's subscription"));
            }

            var subscription = await _subscriptionService.CancelSubscriptionAsync(subscriptionId, request);
            return Ok(ApiResponse<SubscriptionResponse>.Ok(subscription));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<SubscriptionResponse>.WithError(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel subscription {SubscriptionId}", subscriptionId);
            return StatusCode(500, ApiResponse<SubscriptionResponse>.WithError("Failed to cancel subscription"));
        }
    }

    [HttpGet("tenant/{tenantId}/quota")]
    public async Task<ActionResult<ApiResponse<SubscriptionQuota>>> GetQuota(Guid tenantId)
    {
        // Extract tenant_id from JWT (source of truth)
        var jwtTenantId = User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(jwtTenantId))
        {
            return Unauthorized(ApiResponse<SubscriptionQuota>.WithError("Tenant ID not found in token"));
        }

        // Validate route tenant matches JWT tenant
        if (tenantId.ToString() != jwtTenantId)
        {
            _logger.LogWarning("Tenant {TenantId} attempted to access quota for different tenant {RouteTenantId}",
                jwtTenantId, tenantId);
            return StatusCode(403, ApiResponse<SubscriptionQuota>.WithError("Cannot access other tenant's quota"));
        }

        try
        {
            var quota = await _subscriptionService.GetSubscriptionQuotaAsync(tenantId);
            return Ok(ApiResponse<SubscriptionQuota>.Ok(quota));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get quota for tenant {TenantId}", tenantId);
            return StatusCode(500, ApiResponse<SubscriptionQuota>.WithError("Failed to get quota"));
        }
    }

    [HttpPost("tenant/{tenantId}/quota/check")]
    public async Task<ActionResult<ApiResponse<bool>>> CheckQuota(
        Guid tenantId,
        [FromQuery] string quotaType,
        [FromQuery] int amount = 1)
    {
        // Extract tenant_id from JWT (source of truth)
        var jwtTenantId = User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(jwtTenantId))
        {
            return Unauthorized(ApiResponse<bool>.WithError("Tenant ID not found in token"));
        }

        // Validate route tenant matches JWT tenant
        if (tenantId.ToString() != jwtTenantId)
        {
            _logger.LogWarning("Tenant {TenantId} attempted to check quota for different tenant {RouteTenantId}",
                jwtTenantId, tenantId);
            return StatusCode(403, ApiResponse<bool>.WithError("Cannot check other tenant's quota"));
        }

        try
        {
            var isWithinQuota = await _subscriptionService.CheckQuotaAsync(tenantId, quotaType, amount);
            return Ok(ApiResponse<bool>.Ok(isWithinQuota));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check quota for tenant {TenantId}", tenantId);
            return StatusCode(500, ApiResponse<bool>.WithError("Failed to check quota"));
        }
    }

    [HttpPost("tenant/{tenantId}/usage")]
    public async Task<ActionResult<ApiResponse<string>>> RecordUsage(
        Guid tenantId,
        [FromQuery] string usageType,
        [FromQuery] int quantity = 1)
    {
        // Extract tenant_id from JWT (source of truth)
        var jwtTenantId = User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(jwtTenantId))
        {
            return Unauthorized(ApiResponse<string>.WithError("Tenant ID not found in token"));
        }

        // Validate route tenant matches JWT tenant
        if (tenantId.ToString() != jwtTenantId)
        {
            _logger.LogWarning("Tenant {TenantId} attempted to record usage for different tenant {RouteTenantId}",
                jwtTenantId, tenantId);
            return StatusCode(403, ApiResponse<string>.WithError("Cannot record usage for other tenant"));
        }

        try
        {
            await _subscriptionService.RecordUsageAsync(tenantId, usageType, quantity);
            return Ok(ApiResponse<string>.Ok("Usage recorded"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record usage for tenant {TenantId}", tenantId);
            return StatusCode(500, ApiResponse<string>.WithError("Failed to record usage"));
        }
    }
}

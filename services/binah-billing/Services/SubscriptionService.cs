using Binah.Billing.Models;
using Binah.Billing.Models.DTOs;
using Binah.Billing.Repositories;
using System.Text.Json;

namespace Binah.Billing.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly IStripeService _stripeService;
    private readonly ICustomerRepository _customerRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IUsageRepository _usageRepository;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(
        IStripeService stripeService,
        ICustomerRepository customerRepository,
        ISubscriptionRepository subscriptionRepository,
        IUsageRepository usageRepository,
        ILogger<SubscriptionService> logger)
    {
        _stripeService = stripeService;
        _customerRepository = customerRepository;
        _subscriptionRepository = subscriptionRepository;
        _usageRepository = usageRepository;
        _logger = logger;
    }

    public async Task<SubscriptionResponse> CreateSubscriptionAsync(CreateSubscriptionRequest request)
    {
        _logger.LogInformation("Creating subscription for tenant {TenantId} with plan {PlanName}",
            request.TenantId, request.PlanName);

        // Get or create pricing plan
        var plan = PricingPlans.GetPlan(request.PlanName)
            ?? throw new ArgumentException($"Invalid plan name: {request.PlanName}");

        // Get or create customer
        var customer = await _customerRepository.GetByTenantIdAsync(request.TenantId);
        if (customer == null)
        {
            var stripeCustomer = await _stripeService.CreateCustomerAsync(
                request.Email,
                null,
                new Dictionary<string, string> { ["tenant_id"] = request.TenantId.ToString() });

            customer = new Customer
            {
                TenantId = request.TenantId,
                StripeCustomerId = stripeCustomer.Id,
                Email = request.Email
            };

            await _customerRepository.CreateAsync(customer);
        }

        // Create Stripe subscription
        var trialDays = request.StartTrial ? request.TrialDays ?? plan.TrialDays : 0;
        var stripeSubscription = await _stripeService.CreateSubscriptionAsync(
            customer.StripeCustomerId,
            plan.StripePriceId,
            trialDays,
            new Dictionary<string, string>
            {
                ["tenant_id"] = request.TenantId.ToString(),
                ["plan_name"] = plan.Name
            });

        // Create local subscription record
        var subscription = new Subscription
        {
            CustomerId = customer.Id,
            StripeSubscriptionId = stripeSubscription.Id,
            StripePriceId = plan.StripePriceId,
            StripeProductId = plan.StripeProductId,
            PlanName = plan.Name,
            Status = stripeSubscription.Status,
            CurrentPeriodStart = stripeSubscription.CurrentPeriodStart,
            CurrentPeriodEnd = stripeSubscription.CurrentPeriodEnd,
            TrialStart = stripeSubscription.TrialStart,
            TrialEnd = stripeSubscription.TrialEnd,
            Amount = plan.MonthlyPrice,
            Currency = "usd",
            Interval = "month",
            MaxUsers = plan.MaxUsers,
            MaxProperties = plan.MaxEntities,
            MaxApiCallsPerMonth = plan.MaxApiCallsPerMonth,
            Features = JsonSerializer.Serialize(plan.Features)
        };

        await _subscriptionRepository.CreateAsync(subscription);

        _logger.LogInformation("Created subscription {SubscriptionId} for tenant {TenantId}",
            subscription.Id, request.TenantId);

        return MapToResponse(subscription);
    }

    public async Task<SubscriptionResponse> GetSubscriptionAsync(Guid subscriptionId)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId)
            ?? throw new KeyNotFoundException($"Subscription {subscriptionId} not found");

        return MapToResponse(subscription);
    }

    public async Task<SubscriptionResponse?> GetActiveSubscriptionByTenantAsync(Guid tenantId)
    {
        var subscription = await _subscriptionRepository.GetActiveByTenantIdAsync(tenantId);
        return subscription != null ? MapToResponse(subscription) : null;
    }

    public async Task<SubscriptionResponse> UpdateSubscriptionAsync(Guid subscriptionId, UpdateSubscriptionRequest request)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId)
            ?? throw new KeyNotFoundException($"Subscription {subscriptionId} not found");

        // Update plan if requested
        string? newPriceId = null;
        if (request.PlanName != null && request.PlanName != subscription.PlanName)
        {
            var newPlan = PricingPlans.GetPlan(request.PlanName)
                ?? throw new ArgumentException($"Invalid plan name: {request.PlanName}");
            newPriceId = newPlan.StripePriceId;

            subscription.PlanName = newPlan.Name;
            subscription.StripePriceId = newPlan.StripePriceId;
            subscription.StripeProductId = newPlan.StripeProductId;
            subscription.Amount = newPlan.MonthlyPrice;
            subscription.MaxUsers = newPlan.MaxUsers;
            subscription.MaxProperties = newPlan.MaxEntities;
            subscription.MaxApiCallsPerMonth = newPlan.MaxApiCallsPerMonth;
            subscription.Features = JsonSerializer.Serialize(newPlan.Features);
        }

        // Update Stripe subscription
        var stripeSubscription = await _stripeService.UpdateSubscriptionAsync(
            subscription.StripeSubscriptionId,
            newPriceId,
            request.CancelAtPeriodEnd,
            request.Quantity);

        // Update local subscription
        subscription.Status = stripeSubscription.Status;
        subscription.CancelAtPeriodEnd = stripeSubscription.CancelAtPeriodEnd;
        subscription.UpdatedAt = DateTime.UtcNow;

        if (request.Quantity.HasValue)
        {
            subscription.Quantity = request.Quantity.Value;
        }

        await _subscriptionRepository.UpdateAsync(subscription);

        _logger.LogInformation("Updated subscription {SubscriptionId}", subscriptionId);

        return MapToResponse(subscription);
    }

    public async Task<SubscriptionResponse> CancelSubscriptionAsync(Guid subscriptionId, CancelSubscriptionRequest request)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId)
            ?? throw new KeyNotFoundException($"Subscription {subscriptionId} not found");

        var stripeSubscription = await _stripeService.CancelSubscriptionAsync(
            subscription.StripeSubscriptionId,
            request.CancelImmediately);

        subscription.Status = stripeSubscription.Status;
        subscription.CancelAtPeriodEnd = stripeSubscription.CancelAtPeriodEnd;
        subscription.CanceledAt = stripeSubscription.CanceledAt;
        subscription.EndedAt = stripeSubscription.EndedAt;
        subscription.UpdatedAt = DateTime.UtcNow;

        await _subscriptionRepository.UpdateAsync(subscription);

        _logger.LogInformation("Canceled subscription {SubscriptionId}", subscriptionId);

        return MapToResponse(subscription);
    }

    public async Task<List<SubscriptionResponse>> GetSubscriptionHistoryAsync(Guid tenantId)
    {
        var subscriptions = await _subscriptionRepository.GetByTenantIdAsync(tenantId);
        return subscriptions.Select(MapToResponse).ToList();
    }

    public async Task<SubscriptionQuota> GetSubscriptionQuotaAsync(Guid tenantId)
    {
        var subscription = await _subscriptionRepository.GetActiveByTenantIdAsync(tenantId);

        if (subscription == null)
        {
            return new SubscriptionQuota(0, 0, 0, 0, 0, 0, false);
        }

        var currentPeriodStart = subscription.CurrentPeriodStart;
        var currentPeriodEnd = subscription.CurrentPeriodEnd;

        // Get current usage for this period
        var usageRecords = await _usageRepository.GetUsageForPeriodAsync(
            subscription.Id, currentPeriodStart, currentPeriodEnd);

        var currentUsers = usageRecords
            .Where(u => u.UsageType == "users")
            .OrderByDescending(u => u.Timestamp)
            .FirstOrDefault()?.Quantity ?? 0;

        var currentProperties = usageRecords
            .Where(u => u.UsageType == "properties")
            .OrderByDescending(u => u.Timestamp)
            .FirstOrDefault()?.Quantity ?? 0;

        var currentApiCalls = usageRecords
            .Where(u => u.UsageType == "api_calls")
            .Sum(u => u.Quantity);

        var maxUsers = subscription.MaxUsers ?? int.MaxValue;
        var maxProperties = subscription.MaxProperties ?? int.MaxValue;
        var maxApiCalls = subscription.MaxApiCallsPerMonth ?? int.MaxValue;

        var isWithinLimits = (maxUsers == -1 || currentUsers <= maxUsers)
            && (maxProperties == -1 || currentProperties <= maxProperties)
            && (maxApiCalls == -1 || currentApiCalls <= maxApiCalls);

        return new SubscriptionQuota(
            maxUsers,
            maxProperties,
            maxApiCalls,
            currentUsers,
            currentProperties,
            currentApiCalls,
            isWithinLimits
        );
    }

    public async Task<bool> CheckQuotaAsync(Guid tenantId, string quotaType, int requestedAmount = 1)
    {
        var quota = await GetSubscriptionQuotaAsync(tenantId);

        return quotaType switch
        {
            "users" => quota.MaxUsers == -1 || quota.CurrentUsers + requestedAmount <= quota.MaxUsers,
            "properties" => quota.MaxProperties == -1 || quota.CurrentProperties + requestedAmount <= quota.MaxProperties,
            "api_calls" => quota.MaxApiCallsPerMonth == -1 || quota.CurrentApiCallsThisMonth + requestedAmount <= quota.MaxApiCallsPerMonth,
            _ => true
        };
    }

    public async Task RecordUsageAsync(Guid tenantId, string usageType, int quantity)
    {
        var subscription = await _subscriptionRepository.GetActiveByTenantIdAsync(tenantId);
        if (subscription == null)
        {
            _logger.LogWarning("No active subscription for tenant {TenantId}, skipping usage recording", tenantId);
            return;
        }

        var usageRecord = new UsageRecord
        {
            SubscriptionId = subscription.Id,
            TenantId = tenantId,
            UsageType = usageType,
            Quantity = quantity,
            PeriodStart = subscription.CurrentPeriodStart,
            PeriodEnd = subscription.CurrentPeriodEnd
        };

        await _usageRepository.CreateAsync(usageRecord);
    }

    private static SubscriptionResponse MapToResponse(Subscription subscription)
    {
        Dictionary<string, object>? features = null;
        if (!string.IsNullOrEmpty(subscription.Features))
        {
            features = JsonSerializer.Deserialize<Dictionary<string, object>>(subscription.Features);
        }

        return new SubscriptionResponse(
            subscription.Id,
            subscription.CustomerId,
            subscription.StripeSubscriptionId,
            subscription.PlanName,
            subscription.Status,
            subscription.CurrentPeriodStart,
            subscription.CurrentPeriodEnd,
            subscription.TrialStart,
            subscription.TrialEnd,
            subscription.CancelAtPeriodEnd,
            subscription.Amount,
            subscription.Currency,
            subscription.Interval,
            subscription.Quantity,
            subscription.MaxUsers,
            subscription.MaxProperties,
            subscription.MaxApiCallsPerMonth,
            features,
            subscription.CreatedAt,
            subscription.UpdatedAt
        );
    }
}

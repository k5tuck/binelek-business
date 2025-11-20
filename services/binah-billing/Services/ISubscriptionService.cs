using Binah.Billing.Models;
using Binah.Billing.Models.DTOs;

namespace Binah.Billing.Services;

public interface ISubscriptionService
{
    Task<SubscriptionResponse> CreateSubscriptionAsync(CreateSubscriptionRequest request);
    Task<SubscriptionResponse> GetSubscriptionAsync(Guid subscriptionId);
    Task<SubscriptionResponse?> GetActiveSubscriptionByTenantAsync(Guid tenantId);
    Task<SubscriptionResponse> UpdateSubscriptionAsync(Guid subscriptionId, UpdateSubscriptionRequest request);
    Task<SubscriptionResponse> CancelSubscriptionAsync(Guid subscriptionId, CancelSubscriptionRequest request);
    Task<List<SubscriptionResponse>> GetSubscriptionHistoryAsync(Guid tenantId);
    Task<SubscriptionQuota> GetSubscriptionQuotaAsync(Guid tenantId);
    Task<bool> CheckQuotaAsync(Guid tenantId, string quotaType, int requestedAmount = 1);
    Task RecordUsageAsync(Guid tenantId, string usageType, int quantity);
}

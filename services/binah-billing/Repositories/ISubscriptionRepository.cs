using Binah.Billing.Models;

namespace Binah.Billing.Repositories;

public interface ISubscriptionRepository
{
    Task<Subscription?> GetByIdAsync(Guid id);
    Task<Subscription?> GetActiveByTenantIdAsync(Guid tenantId);
    Task<List<Subscription>> GetByTenantIdAsync(Guid tenantId);
    Task<Subscription?> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId);
    Task<Subscription> CreateAsync(Subscription subscription);
    Task<Subscription> UpdateAsync(Subscription subscription);
    Task DeleteAsync(Guid id);
}

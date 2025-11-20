using Binah.Billing.Models;
using Microsoft.EntityFrameworkCore;

namespace Binah.Billing.Repositories;

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly BillingDbContext _context;

    public SubscriptionRepository(BillingDbContext context)
    {
        _context = context;
    }

    public async Task<Subscription?> GetByIdAsync(Guid id)
    {
        return await _context.Subscriptions
            .Include(s => s.Customer)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Subscription?> GetActiveByTenantIdAsync(Guid tenantId)
    {
        return await _context.Subscriptions
            .Include(s => s.Customer)
            .Where(s => s.Customer.TenantId == tenantId)
            .Where(s => s.Status == "active" || s.Status == "trialing")
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Subscription>> GetByTenantIdAsync(Guid tenantId)
    {
        return await _context.Subscriptions
            .Include(s => s.Customer)
            .Where(s => s.Customer.TenantId == tenantId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<Subscription?> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId)
    {
        return await _context.Subscriptions
            .Include(s => s.Customer)
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscriptionId);
    }

    public async Task<Subscription> CreateAsync(Subscription subscription)
    {
        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();
        return subscription;
    }

    public async Task<Subscription> UpdateAsync(Subscription subscription)
    {
        subscription.UpdatedAt = DateTime.UtcNow;
        _context.Subscriptions.Update(subscription);
        await _context.SaveChangesAsync();
        return subscription;
    }

    public async Task DeleteAsync(Guid id)
    {
        var subscription = await GetByIdAsync(id);
        if (subscription != null)
        {
            subscription.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}

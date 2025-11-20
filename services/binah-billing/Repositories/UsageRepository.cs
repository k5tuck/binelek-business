using Binah.Billing.Models;
using Microsoft.EntityFrameworkCore;

namespace Binah.Billing.Repositories;

public class UsageRepository : IUsageRepository
{
    private readonly BillingDbContext _context;

    public UsageRepository(BillingDbContext context)
    {
        _context = context;
    }

    public async Task<UsageRecord> CreateAsync(UsageRecord usageRecord)
    {
        _context.UsageRecords.Add(usageRecord);
        await _context.SaveChangesAsync();
        return usageRecord;
    }

    public async Task<List<UsageRecord>> GetUsageForPeriodAsync(Guid subscriptionId, DateTime periodStart, DateTime periodEnd)
    {
        return await _context.UsageRecords
            .Where(u => u.SubscriptionId == subscriptionId)
            .Where(u => u.Timestamp >= periodStart && u.Timestamp <= periodEnd)
            .OrderBy(u => u.Timestamp)
            .ToListAsync();
    }

    public async Task<List<UsageRecord>> GetUsageByTypeAsync(Guid tenantId, string usageType, DateTime? startDate, DateTime? endDate)
    {
        var query = _context.UsageRecords
            .Where(u => u.TenantId == tenantId)
            .Where(u => u.UsageType == usageType);

        if (startDate.HasValue)
        {
            query = query.Where(u => u.Timestamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(u => u.Timestamp <= endDate.Value);
        }

        return await query
            .OrderBy(u => u.Timestamp)
            .ToListAsync();
    }
}

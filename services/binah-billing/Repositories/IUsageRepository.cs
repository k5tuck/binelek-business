using Binah.Billing.Models;

namespace Binah.Billing.Repositories;

public interface IUsageRepository
{
    Task<UsageRecord> CreateAsync(UsageRecord usageRecord);
    Task<List<UsageRecord>> GetUsageForPeriodAsync(Guid subscriptionId, DateTime periodStart, DateTime periodEnd);
    Task<List<UsageRecord>> GetUsageByTypeAsync(Guid tenantId, string usageType, DateTime? startDate, DateTime? endDate);
}

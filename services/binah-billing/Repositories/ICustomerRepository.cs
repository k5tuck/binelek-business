using Binah.Billing.Models;

namespace Binah.Billing.Repositories;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id);
    Task<Customer?> GetByTenantIdAsync(Guid tenantId);
    Task<Customer?> GetByStripeCustomerIdAsync(string stripeCustomerId);
    Task<Customer> CreateAsync(Customer customer);
    Task<Customer> UpdateAsync(Customer customer);
    Task DeleteAsync(Guid id);
}

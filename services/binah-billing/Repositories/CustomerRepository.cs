using Binah.Billing.Models;
using Microsoft.EntityFrameworkCore;

namespace Binah.Billing.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly BillingDbContext _context;

    public CustomerRepository(BillingDbContext context)
    {
        _context = context;
    }

    public async Task<Customer?> GetByIdAsync(Guid id)
    {
        return await _context.Customers
            .Include(c => c.Subscriptions)
            .Include(c => c.PaymentMethods)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Customer?> GetByTenantIdAsync(Guid tenantId)
    {
        return await _context.Customers
            .Include(c => c.Subscriptions)
            .Include(c => c.PaymentMethods)
            .FirstOrDefaultAsync(c => c.TenantId == tenantId);
    }

    public async Task<Customer?> GetByStripeCustomerIdAsync(string stripeCustomerId)
    {
        return await _context.Customers
            .Include(c => c.Subscriptions)
            .Include(c => c.PaymentMethods)
            .FirstOrDefaultAsync(c => c.StripeCustomerId == stripeCustomerId);
    }

    public async Task<Customer> CreateAsync(Customer customer)
    {
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        return customer;
    }

    public async Task<Customer> UpdateAsync(Customer customer)
    {
        customer.UpdatedAt = DateTime.UtcNow;
        _context.Customers.Update(customer);
        await _context.SaveChangesAsync();
        return customer;
    }

    public async Task DeleteAsync(Guid id)
    {
        var customer = await GetByIdAsync(id);
        if (customer != null)
        {
            customer.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}

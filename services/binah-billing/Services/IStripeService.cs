using Stripe;

namespace Binah.Billing.Services;

public interface IStripeService
{
    Task<Customer> CreateCustomerAsync(string email, string? name, Dictionary<string, string>? metadata);
    Task<Customer> UpdateCustomerAsync(string customerId, string? email, string? name);
    Task<Customer> GetCustomerAsync(string customerId);

    Task<Stripe.Subscription> CreateSubscriptionAsync(
        string customerId,
        string priceId,
        int? trialDays = null,
        Dictionary<string, string>? metadata = null);

    Task<Stripe.Subscription> UpdateSubscriptionAsync(
        string subscriptionId,
        string? priceId = null,
        bool? cancelAtPeriodEnd = null,
        int? quantity = null);

    Task<Stripe.Subscription> CancelSubscriptionAsync(string subscriptionId, bool cancelImmediately);
    Task<Stripe.Subscription> GetSubscriptionAsync(string subscriptionId);

    Task<PaymentMethod> AttachPaymentMethodAsync(string paymentMethodId, string customerId);
    Task<PaymentMethod> DetachPaymentMethodAsync(string paymentMethodId);
    Task<Customer> SetDefaultPaymentMethodAsync(string customerId, string paymentMethodId);

    Task<Invoice> GetInvoiceAsync(string invoiceId);
    Task<StripeList<Invoice>> ListInvoicesAsync(string? customerId = null, int limit = 100);

    Task<Price> CreatePriceAsync(string productId, long amount, string currency, string interval);
    Task<Product> CreateProductAsync(string name, string description);
}

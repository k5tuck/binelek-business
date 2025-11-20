using Stripe;

namespace Binah.Billing.Services;

public class StripeService : IStripeService
{
    private readonly Stripe.CustomerService _customerService;
    private readonly Stripe.SubscriptionService _subscriptionService;
    private readonly Stripe.PaymentMethodService _paymentMethodService;
    private readonly Stripe.InvoiceService _invoiceService;
    private readonly Stripe.PriceService _priceService;
    private readonly Stripe.ProductService _productService;
    private readonly ILogger<StripeService> _logger;

    public StripeService(IConfiguration configuration, ILogger<StripeService> logger)
    {
        _logger = logger;

        var apiKey = configuration["Stripe:SecretKey"]
            ?? throw new InvalidOperationException("Stripe:SecretKey not configured");

        StripeConfiguration.ApiKey = apiKey;

        _customerService = new Stripe.CustomerService();
        _subscriptionService = new Stripe.SubscriptionService();
        _paymentMethodService = new Stripe.PaymentMethodService();
        _invoiceService = new Stripe.InvoiceService();
        _priceService = new Stripe.PriceService();
        _productService = new Stripe.ProductService();
    }

    public async Task<Customer> CreateCustomerAsync(string email, string? name, Dictionary<string, string>? metadata)
    {
        try
        {
            var options = new CustomerCreateOptions
            {
                Email = email,
                Name = name,
                Metadata = metadata
            };

            var customer = await _customerService.CreateAsync(options);
            _logger.LogInformation("Created Stripe customer {CustomerId} for email {Email}", customer.Id, email);
            return customer;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to create Stripe customer for email {Email}", email);
            throw;
        }
    }

    public async Task<Customer> UpdateCustomerAsync(string customerId, string? email, string? name)
    {
        try
        {
            var options = new CustomerUpdateOptions
            {
                Email = email,
                Name = name
            };

            var customer = await _customerService.UpdateAsync(customerId, options);
            _logger.LogInformation("Updated Stripe customer {CustomerId}", customerId);
            return customer;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to update Stripe customer {CustomerId}", customerId);
            throw;
        }
    }

    public async Task<Customer> GetCustomerAsync(string customerId)
    {
        try
        {
            return await _customerService.GetAsync(customerId);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to get Stripe customer {CustomerId}", customerId);
            throw;
        }
    }

    public async Task<Stripe.Subscription> CreateSubscriptionAsync(
        string customerId,
        string priceId,
        int? trialDays = null,
        Dictionary<string, string>? metadata = null)
    {
        try
        {
            var options = new SubscriptionCreateOptions
            {
                Customer = customerId,
                Items = new List<SubscriptionItemOptions>
                {
                    new SubscriptionItemOptions { Price = priceId }
                },
                Metadata = metadata,
                PaymentBehavior = "default_incomplete",
                PaymentSettings = new SubscriptionPaymentSettingsOptions
                {
                    SaveDefaultPaymentMethod = "on_subscription"
                },
                Expand = new List<string> { "latest_invoice.payment_intent" }
            };

            if (trialDays.HasValue && trialDays.Value > 0)
            {
                options.TrialPeriodDays = trialDays.Value;
            }

            var subscription = await _subscriptionService.CreateAsync(options);
            _logger.LogInformation("Created Stripe subscription {SubscriptionId} for customer {CustomerId}",
                subscription.Id, customerId);
            return subscription;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to create Stripe subscription for customer {CustomerId}", customerId);
            throw;
        }
    }

    public async Task<Stripe.Subscription> UpdateSubscriptionAsync(
        string subscriptionId,
        string? priceId = null,
        bool? cancelAtPeriodEnd = null,
        int? quantity = null)
    {
        try
        {
            var options = new SubscriptionUpdateOptions
            {
                CancelAtPeriodEnd = cancelAtPeriodEnd
            };

            if (priceId != null)
            {
                // Get existing subscription to update items
                var existingSub = await _subscriptionService.GetAsync(subscriptionId);
                options.Items = new List<SubscriptionItemOptions>
                {
                    new SubscriptionItemOptions
                    {
                        Id = existingSub.Items.Data[0].Id,
                        Price = priceId,
                        Quantity = quantity
                    }
                };
            }
            else if (quantity.HasValue)
            {
                var existingSub = await _subscriptionService.GetAsync(subscriptionId);
                options.Items = new List<SubscriptionItemOptions>
                {
                    new SubscriptionItemOptions
                    {
                        Id = existingSub.Items.Data[0].Id,
                        Quantity = quantity.Value
                    }
                };
            }

            var subscription = await _subscriptionService.UpdateAsync(subscriptionId, options);
            _logger.LogInformation("Updated Stripe subscription {SubscriptionId}", subscriptionId);
            return subscription;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to update Stripe subscription {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    public async Task<Stripe.Subscription> CancelSubscriptionAsync(string subscriptionId, bool cancelImmediately)
    {
        try
        {
            if (cancelImmediately)
            {
                var subscription = await _subscriptionService.CancelAsync(subscriptionId);
                _logger.LogInformation("Canceled Stripe subscription {SubscriptionId} immediately", subscriptionId);
                return subscription;
            }
            else
            {
                var options = new SubscriptionUpdateOptions
                {
                    CancelAtPeriodEnd = true
                };
                var subscription = await _subscriptionService.UpdateAsync(subscriptionId, options);
                _logger.LogInformation("Scheduled Stripe subscription {SubscriptionId} for cancellation", subscriptionId);
                return subscription;
            }
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to cancel Stripe subscription {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    public async Task<Stripe.Subscription> GetSubscriptionAsync(string subscriptionId)
    {
        try
        {
            return await _subscriptionService.GetAsync(subscriptionId);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to get Stripe subscription {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    public async Task<PaymentMethod> AttachPaymentMethodAsync(string paymentMethodId, string customerId)
    {
        try
        {
            var options = new PaymentMethodAttachOptions
            {
                Customer = customerId
            };

            var paymentMethod = await _paymentMethodService.AttachAsync(paymentMethodId, options);
            _logger.LogInformation("Attached payment method {PaymentMethodId} to customer {CustomerId}",
                paymentMethodId, customerId);
            return paymentMethod;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to attach payment method {PaymentMethodId} to customer {CustomerId}",
                paymentMethodId, customerId);
            throw;
        }
    }

    public async Task<PaymentMethod> DetachPaymentMethodAsync(string paymentMethodId)
    {
        try
        {
            var paymentMethod = await _paymentMethodService.DetachAsync(paymentMethodId);
            _logger.LogInformation("Detached payment method {PaymentMethodId}", paymentMethodId);
            return paymentMethod;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to detach payment method {PaymentMethodId}", paymentMethodId);
            throw;
        }
    }

    public async Task<Customer> SetDefaultPaymentMethodAsync(string customerId, string paymentMethodId)
    {
        try
        {
            var options = new CustomerUpdateOptions
            {
                InvoiceSettings = new CustomerInvoiceSettingsOptions
                {
                    DefaultPaymentMethod = paymentMethodId
                }
            };

            var customer = await _customerService.UpdateAsync(customerId, options);
            _logger.LogInformation("Set default payment method {PaymentMethodId} for customer {CustomerId}",
                paymentMethodId, customerId);
            return customer;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to set default payment method for customer {CustomerId}", customerId);
            throw;
        }
    }

    public async Task<Invoice> GetInvoiceAsync(string invoiceId)
    {
        try
        {
            return await _invoiceService.GetAsync(invoiceId);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to get invoice {InvoiceId}", invoiceId);
            throw;
        }
    }

    public async Task<StripeList<Invoice>> ListInvoicesAsync(string? customerId = null, int limit = 100)
    {
        try
        {
            var options = new InvoiceListOptions
            {
                Limit = limit,
                Customer = customerId
            };

            return await _invoiceService.ListAsync(options);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to list invoices");
            throw;
        }
    }

    public async Task<Price> CreatePriceAsync(string productId, long amount, string currency, string interval)
    {
        try
        {
            var options = new PriceCreateOptions
            {
                Product = productId,
                UnitAmount = amount,
                Currency = currency,
                Recurring = new PriceRecurringOptions
                {
                    Interval = interval
                }
            };

            var price = await _priceService.CreateAsync(options);
            _logger.LogInformation("Created Stripe price {PriceId} for product {ProductId}", price.Id, productId);
            return price;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to create Stripe price for product {ProductId}", productId);
            throw;
        }
    }

    public async Task<Product> CreateProductAsync(string name, string description)
    {
        try
        {
            var options = new ProductCreateOptions
            {
                Name = name,
                Description = description
            };

            var product = await _productService.CreateAsync(options);
            _logger.LogInformation("Created Stripe product {ProductId}", product.Id);
            return product;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to create Stripe product {Name}", name);
            throw;
        }
    }
}

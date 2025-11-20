using Binah.Billing.Models;
using Binah.Billing.Repositories;
using Stripe;
using System.Text.Json;

namespace Binah.Billing.Services;

public class WebhookService : IWebhookService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(
        ICustomerRepository customerRepository,
        ISubscriptionRepository subscriptionRepository,
        ILogger<WebhookService> logger)
    {
        _customerRepository = customerRepository;
        _subscriptionRepository = subscriptionRepository;
        _logger = logger;
    }

    public async Task HandleWebhookAsync(Event stripeEvent)
    {
        try
        {
            switch (stripeEvent.Type)
            {
                case Events.CustomerCreated:
                    await HandleCustomerCreatedAsync(stripeEvent);
                    break;

                case Events.CustomerUpdated:
                    await HandleCustomerUpdatedAsync(stripeEvent);
                    break;

                case Events.CustomerDeleted:
                    await HandleCustomerDeletedAsync(stripeEvent);
                    break;

                case Events.CustomerSubscriptionCreated:
                    await HandleSubscriptionCreatedAsync(stripeEvent);
                    break;

                case Events.CustomerSubscriptionUpdated:
                    await HandleSubscriptionUpdatedAsync(stripeEvent);
                    break;

                case Events.CustomerSubscriptionDeleted:
                    await HandleSubscriptionDeletedAsync(stripeEvent);
                    break;

                case Events.CustomerSubscriptionTrialWillEnd:
                    await HandleSubscriptionTrialWillEndAsync(stripeEvent);
                    break;

                case Events.InvoiceCreated:
                    await HandleInvoiceCreatedAsync(stripeEvent);
                    break;

                case Events.InvoicePaymentSucceeded:
                    await HandleInvoicePaymentSucceededAsync(stripeEvent);
                    break;

                case Events.InvoicePaymentFailed:
                    await HandleInvoicePaymentFailedAsync(stripeEvent);
                    break;

                case Events.PaymentMethodAttached:
                    await HandlePaymentMethodAttachedAsync(stripeEvent);
                    break;

                case Events.PaymentMethodDetached:
                    await HandlePaymentMethodDetachedAsync(stripeEvent);
                    break;

                default:
                    _logger.LogInformation("Unhandled webhook event type: {EventType}", stripeEvent.Type);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling webhook event {EventType}", stripeEvent.Type);
            throw;
        }
    }

    private async Task HandleCustomerCreatedAsync(Event stripeEvent)
    {
        var customer = stripeEvent.Data.Object as Stripe.Customer;
        if (customer == null) return;

        _logger.LogInformation("Customer created: {CustomerId}", customer.Id);
        // Customer is already created in SubscriptionService.CreateSubscriptionAsync
    }

    private async Task HandleCustomerUpdatedAsync(Event stripeEvent)
    {
        var customer = stripeEvent.Data.Object as Stripe.Customer;
        if (customer == null) return;

        var localCustomer = await _customerRepository.GetByStripeCustomerIdAsync(customer.Id);
        if (localCustomer != null)
        {
            localCustomer.Email = customer.Email ?? localCustomer.Email;
            localCustomer.Name = customer.Name ?? localCustomer.Name;
            localCustomer.Phone = customer.Phone ?? localCustomer.Phone;
            await _customerRepository.UpdateAsync(localCustomer);

            _logger.LogInformation("Updated customer: {CustomerId}", customer.Id);
        }
    }

    private async Task HandleCustomerDeletedAsync(Event stripeEvent)
    {
        var customer = stripeEvent.Data.Object as Stripe.Customer;
        if (customer == null) return;

        var localCustomer = await _customerRepository.GetByStripeCustomerIdAsync(customer.Id);
        if (localCustomer != null)
        {
            await _customerRepository.DeleteAsync(localCustomer.Id);
            _logger.LogInformation("Deleted customer: {CustomerId}", customer.Id);
        }
    }

    private async Task HandleSubscriptionCreatedAsync(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (subscription == null) return;

        _logger.LogInformation("Subscription created: {SubscriptionId}", subscription.Id);
        // Subscription is already created in SubscriptionService.CreateSubscriptionAsync
    }

    private async Task HandleSubscriptionUpdatedAsync(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (subscription == null) return;

        var localSubscription = await _subscriptionRepository.GetByStripeSubscriptionIdAsync(subscription.Id);
        if (localSubscription != null)
        {
            localSubscription.Status = subscription.Status;
            localSubscription.CurrentPeriodStart = subscription.CurrentPeriodStart;
            localSubscription.CurrentPeriodEnd = subscription.CurrentPeriodEnd;
            localSubscription.CancelAtPeriodEnd = subscription.CancelAtPeriodEnd;
            localSubscription.CanceledAt = subscription.CanceledAt;
            localSubscription.EndedAt = subscription.EndedAt;

            await _subscriptionRepository.UpdateAsync(localSubscription);

            _logger.LogInformation("Updated subscription: {SubscriptionId}, Status: {Status}",
                subscription.Id, subscription.Status);
        }
    }

    private async Task HandleSubscriptionDeletedAsync(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (subscription == null) return;

        var localSubscription = await _subscriptionRepository.GetByStripeSubscriptionIdAsync(subscription.Id);
        if (localSubscription != null)
        {
            localSubscription.Status = "canceled";
            localSubscription.EndedAt = DateTime.UtcNow;
            await _subscriptionRepository.UpdateAsync(localSubscription);

            _logger.LogInformation("Subscription deleted: {SubscriptionId}", subscription.Id);
        }
    }

    private async Task HandleSubscriptionTrialWillEndAsync(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (subscription == null) return;

        _logger.LogInformation("Subscription trial will end: {SubscriptionId}, TrialEnd: {TrialEnd}",
            subscription.Id, subscription.TrialEnd);

        // TODO: Send email notification to customer
    }

    private async Task HandleInvoiceCreatedAsync(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Stripe.Invoice;
        if (invoice == null) return;

        _logger.LogInformation("Invoice created: {InvoiceId}, Status: {Status}",
            invoice.Id, invoice.Status);
    }

    private async Task HandleInvoicePaymentSucceededAsync(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Stripe.Invoice;
        if (invoice == null) return;

        _logger.LogInformation("Invoice payment succeeded: {InvoiceId}, Amount: {Amount}",
            invoice.Id, invoice.AmountPaid);

        // Update subscription status if needed
        if (invoice.SubscriptionId != null)
        {
            var subscription = await _subscriptionRepository.GetByStripeSubscriptionIdAsync(invoice.SubscriptionId);
            if (subscription != null && subscription.Status != "active")
            {
                subscription.Status = "active";
                await _subscriptionRepository.UpdateAsync(subscription);
            }
        }
    }

    private async Task HandleInvoicePaymentFailedAsync(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Stripe.Invoice;
        if (invoice == null) return;

        _logger.LogWarning("Invoice payment failed: {InvoiceId}, Attempt: {Attempt}",
            invoice.Id, invoice.AttemptCount);

        // Update subscription status
        if (invoice.SubscriptionId != null)
        {
            var subscription = await _subscriptionRepository.GetByStripeSubscriptionIdAsync(invoice.SubscriptionId);
            if (subscription != null)
            {
                subscription.Status = "past_due";
                await _subscriptionRepository.UpdateAsync(subscription);
            }
        }

        // TODO: Send email notification to customer
    }

    private async Task HandlePaymentMethodAttachedAsync(Event stripeEvent)
    {
        var paymentMethod = stripeEvent.Data.Object as Stripe.PaymentMethod;
        if (paymentMethod == null) return;

        _logger.LogInformation("Payment method attached: {PaymentMethodId}, Customer: {CustomerId}",
            paymentMethod.Id, paymentMethod.CustomerId);
    }

    private async Task HandlePaymentMethodDetachedAsync(Event stripeEvent)
    {
        var paymentMethod = stripeEvent.Data.Object as Stripe.PaymentMethod;
        if (paymentMethod == null) return;

        _logger.LogInformation("Payment method detached: {PaymentMethodId}", paymentMethod.Id);
    }
}

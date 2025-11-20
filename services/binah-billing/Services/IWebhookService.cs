using Stripe;

namespace Binah.Billing.Services;

public interface IWebhookService
{
    Task HandleWebhookAsync(Event stripeEvent);
}

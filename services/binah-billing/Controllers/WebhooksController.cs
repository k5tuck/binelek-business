using Binah.Billing.Services;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace Binah.Billing.Controllers;

[ApiController]
[Route("api/billing/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly IWebhookService _webhookService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(
        IWebhookService webhookService,
        IConfiguration configuration,
        ILogger<WebhooksController> logger)
    {
        _webhookService = webhookService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("stripe")]
    public async Task<IActionResult> HandleStripeWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

        try
        {
            var webhookSecret = _configuration["Stripe:WebhookSecret"];
            var stripeSignature = Request.Headers["Stripe-Signature"].ToString();

            Event stripeEvent;

            if (!string.IsNullOrEmpty(webhookSecret))
            {
                stripeEvent = EventUtility.ConstructEvent(
                    json,
                    stripeSignature,
                    webhookSecret
                );
            }
            else
            {
                stripeEvent = EventUtility.ParseEvent(json);
            }

            _logger.LogInformation("Received Stripe webhook: {EventType}", stripeEvent.Type);

            await _webhookService.HandleWebhookAsync(stripeEvent);

            return Ok();
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe webhook error");
            return BadRequest();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook handling error");
            return StatusCode(500);
        }
    }
}

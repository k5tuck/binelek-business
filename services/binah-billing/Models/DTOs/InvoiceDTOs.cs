namespace Binah.Billing.Models.DTOs;

public record InvoiceResponse(
    Guid Id,
    Guid CustomerId,
    Guid? SubscriptionId,
    string StripeInvoiceId,
    string? InvoiceNumber,
    string Status,
    decimal AmountDue,
    decimal AmountPaid,
    decimal AmountRemaining,
    decimal Total,
    string Currency,
    DateTime? PeriodStart,
    DateTime? PeriodEnd,
    DateTime? DueDate,
    DateTime? PaidAt,
    string? HostedInvoiceUrl,
    string? InvoicePdfUrl,
    DateTime CreatedAt
);

public record InvoiceListRequest(
    Guid? CustomerId = null,
    Guid? SubscriptionId = null,
    string? Status = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    int PageSize = 20,
    int Page = 1
);

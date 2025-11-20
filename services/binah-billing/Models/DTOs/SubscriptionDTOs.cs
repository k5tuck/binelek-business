namespace Binah.Billing.Models.DTOs;

public record CreateSubscriptionRequest(
    Guid TenantId,
    string Email,
    string PlanName, // basic, pro, enterprise
    string? PaymentMethodId = null,
    bool StartTrial = false,
    int? TrialDays = 14
);

public record UpdateSubscriptionRequest(
    string? PlanName = null,
    bool? CancelAtPeriodEnd = null,
    int? Quantity = null
);

public record SubscriptionResponse(
    Guid Id,
    Guid CustomerId,
    string StripeSubscriptionId,
    string PlanName,
    string Status,
    DateTime CurrentPeriodStart,
    DateTime CurrentPeriodEnd,
    DateTime? TrialStart,
    DateTime? TrialEnd,
    bool CancelAtPeriodEnd,
    decimal Amount,
    string Currency,
    string Interval,
    int Quantity,
    int? MaxUsers,
    int? MaxProperties,
    int? MaxApiCallsPerMonth,
    Dictionary<string, object>? Features,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CancelSubscriptionRequest(
    bool CancelImmediately = false
);

public record SubscriptionQuota(
    int MaxUsers,
    int MaxProperties,
    int MaxApiCallsPerMonth,
    int CurrentUsers,
    int CurrentProperties,
    int CurrentApiCallsThisMonth,
    bool IsWithinLimits
);

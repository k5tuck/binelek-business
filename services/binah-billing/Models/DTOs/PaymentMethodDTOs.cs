namespace Binah.Billing.Models.DTOs;

public record AttachPaymentMethodRequest(
    string StripePaymentMethodId,
    bool SetAsDefault = false
);

public record PaymentMethodResponse(
    Guid Id,
    Guid CustomerId,
    string StripePaymentMethodId,
    string Type,
    bool IsDefault,
    CardInfo? Card,
    DateTime CreatedAt
);

public record CardInfo(
    string Brand,
    string Last4,
    int ExpMonth,
    int ExpYear
);

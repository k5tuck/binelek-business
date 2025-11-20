namespace Binah.Billing.Models.DTOs;

public record CreateCustomerRequest(
    Guid TenantId,
    string Email,
    string? Name = null,
    string? Phone = null,
    AddressInfo? Address = null
);

public record UpdateCustomerRequest(
    string? Email = null,
    string? Name = null,
    string? Phone = null,
    AddressInfo? Address = null
);

public record CustomerResponse(
    Guid Id,
    Guid TenantId,
    string StripeCustomerId,
    string Email,
    string? Name,
    string? Phone,
    AddressInfo? Address,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record AddressInfo(
    string? Line1 = null,
    string? Line2 = null,
    string? City = null,
    string? State = null,
    string? PostalCode = null,
    string? Country = null
);

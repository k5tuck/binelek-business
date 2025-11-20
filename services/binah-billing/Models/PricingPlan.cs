namespace Binah.Billing.Models;

public class PricingPlan
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string StripePriceId { get; set; } = string.Empty;
    public string StripeProductId { get; set; } = string.Empty;

    // Hybrid Pricing Model
    public decimal BaseMonthlyPrice { get; set; }  // Base platform fee
    public decimal BaseYearlyPrice { get; set; }   // Base platform fee (annual)
    public decimal PricePerUser { get; set; }      // Additional cost per user beyond included
    public int IncludedUsers { get; set; }         // Number of users included in base price

    // Legacy pricing (for backwards compatibility)
    [Obsolete("Use BaseMonthlyPrice instead")]
    public decimal MonthlyPrice { get; set; }
    [Obsolete("Use BaseYearlyPrice instead")]
    public decimal YearlyPrice { get; set; }

    // Limits
    public int MaxUsers { get; set; }              // -1 for unlimited
    public int MaxEntities { get; set; }           // Renamed from MaxProperties for domain-agnostic
    public int MaxApiCallsPerMonth { get; set; }   // -1 for unlimited

    // Overage Pricing
    public decimal ApiCallOverageRate { get; set; } = 0.10m;  // Per 1,000 calls
    public decimal EntityOverageRate { get; set; } = 0.05m;   // Per 100 entities

    public Dictionary<string, bool> Features { get; set; } = new();
    public int TrialDays { get; set; } = 14;

    /// <summary>
    /// Calculate monthly charge for a given usage
    /// </summary>
    public decimal CalculateMonthlyCharge(int activeUsers, long apiCallsUsed, int entitiesStored)
    {
        decimal charge = BaseMonthlyPrice;

        // Add per-user charges
        if (MaxUsers != -1 && activeUsers > IncludedUsers)
        {
            int additionalUsers = activeUsers - IncludedUsers;
            charge += additionalUsers * PricePerUser;
        }

        // Add API overage charges (if there's a limit)
        if (MaxApiCallsPerMonth > 0 && apiCallsUsed > MaxApiCallsPerMonth)
        {
            long overageCalls = apiCallsUsed - MaxApiCallsPerMonth;
            charge += (overageCalls / 1000m) * ApiCallOverageRate;
        }

        // Add entity overage charges (if there's a limit)
        if (MaxEntities > 0 && entitiesStored > MaxEntities)
        {
            int overageEntities = entitiesStored - MaxEntities;
            charge += (overageEntities / 100m) * EntityOverageRate;
        }

        return charge;
    }
}

public static class PricingPlans
{
    public static readonly Dictionary<string, PricingPlan> Plans = new()
    {
        ["developer"] = new PricingPlan
        {
            Name = "developer",
            DisplayName = "Developer Sandbox",
            StripePriceId = "", // No Stripe price for sandbox
            StripeProductId = "",
            BaseMonthlyPrice = 0.00m,
            BaseYearlyPrice = 0.00m,
            PricePerUser = 0.00m,
            IncludedUsers = 1,
            MaxUsers = 1,
            MaxEntities = 500,
            MaxApiCallsPerMonth = 10000,
            TrialDays = 0,
            Features = new Dictionary<string, bool>
            {
                ["ontology_management"] = true,
                ["basic_search"] = true,
                ["api_access"] = true,
                ["public_data_only"] = true,  // No sensitive data allowed
                ["powered_by_branding"] = true, // Shows "Powered by Binelek"
                ["data_pipeline"] = false,
                ["ai_insights"] = false,
                ["priority_support"] = false,
                ["custom_ontology"] = false,
                ["sso"] = false
            }
        },
        ["starter"] = new PricingPlan
        {
            Name = "starter",
            DisplayName = "Starter",
            StripePriceId = "price_starter_monthly",
            StripeProductId = "prod_starter",
            BaseMonthlyPrice = 499.00m,
            BaseYearlyPrice = 4990.00m,
            PricePerUser = 35.00m,
            IncludedUsers = 5,
            MaxUsers = 50,
            MaxEntities = 5000,
            MaxApiCallsPerMonth = 100000,
            TrialDays = 14,
            Features = new Dictionary<string, bool>
            {
                ["ontology_management"] = true,
                ["basic_search"] = true,
                ["advanced_search"] = true,
                ["api_access"] = true,
                ["data_pipeline"] = true,
                ["ai_insights"] = false,
                ["priority_support"] = false,
                ["custom_ontology"] = true,
                ["sso"] = false,
                ["webhooks"] = true,
                ["analytics"] = true
            }
        },
        ["professional"] = new PricingPlan
        {
            Name = "professional",
            DisplayName = "Professional",
            StripePriceId = "price_pro_monthly",
            StripeProductId = "prod_pro",
            BaseMonthlyPrice = 1499.00m,
            BaseYearlyPrice = 14990.00m,
            PricePerUser = 60.00m,
            IncludedUsers = 15,
            MaxUsers = 200,
            MaxEntities = 50000,
            MaxApiCallsPerMonth = 1000000,
            TrialDays = 14,
            Features = new Dictionary<string, bool>
            {
                ["ontology_management"] = true,
                ["basic_search"] = true,
                ["advanced_search"] = true,
                ["api_access"] = true,
                ["data_pipeline"] = true,
                ["ai_insights"] = true,
                ["priority_support"] = true,
                ["custom_ontology"] = true,
                ["sso"] = true,  // SAML SSO included
                ["webhooks"] = true,
                ["analytics"] = true,
                ["audit_logs"] = true,
                ["compliance_reports"] = true
            }
        },
        ["enterprise"] = new PricingPlan
        {
            Name = "enterprise",
            DisplayName = "Enterprise",
            StripePriceId = "price_enterprise_monthly",
            StripeProductId = "prod_enterprise",
            BaseMonthlyPrice = 4999.00m,
            BaseYearlyPrice = 49990.00m,
            PricePerUser = 0.00m,  // Custom pricing, negotiated per contract
            IncludedUsers = -1,  // Unlimited
            MaxUsers = -1, // Unlimited
            MaxEntities = -1, // Unlimited
            MaxApiCallsPerMonth = -1, // Unlimited
            TrialDays = 14,
            Features = new Dictionary<string, bool>
            {
                ["ontology_management"] = true,
                ["basic_search"] = true,
                ["advanced_search"] = true,
                ["api_access"] = true,
                ["data_pipeline"] = true,
                ["ai_insights"] = true,
                ["priority_support"] = true,
                ["custom_ontology"] = true,
                ["sso"] = true,
                ["webhooks"] = true,
                ["analytics"] = true,
                ["dedicated_support"] = true,
                ["custom_integrations"] = true,
                ["audit_logs"] = true,
                ["compliance"] = true,
                ["white_label"] = true,
                ["sla_guarantee"] = true,
                ["on_premise_option"] = true
            }
        }
    };

    public static PricingPlan? GetPlan(string planName)
    {
        return Plans.TryGetValue(planName.ToLower(), out var plan) ? plan : null;
    }

    /// <summary>
    /// Get all available plans
    /// </summary>
    public static List<PricingPlan> GetAllPlans()
    {
        return Plans.Values.ToList();
    }

    /// <summary>
    /// Get display name for a plan
    /// </summary>
    public static string GetDisplayName(string planName)
    {
        var plan = GetPlan(planName);
        return plan?.DisplayName ?? planName;
    }
}

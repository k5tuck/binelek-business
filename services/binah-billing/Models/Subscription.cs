using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Binah.Billing.Models;

/// <summary>
/// Subscription tier levels for the platform
/// </summary>
public enum SubscriptionTier
{
    /// <summary>
    /// Solo tier - Individual professionals ($29/mo)
    /// </summary>
    Solo = 0,

    /// <summary>
    /// Team tier - Small teams 2-10 ($199/mo)
    /// </summary>
    Team = 1,

    /// <summary>
    /// Business tier - Growing companies 10-50 ($799/mo)
    /// </summary>
    Business = 2,

    /// <summary>
    /// Enterprise tier - Large organizations 50+ (Custom pricing)
    /// </summary>
    Enterprise = 3
}

[Table("subscriptions")]
public class Subscription
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("customer_id")]
    [Required]
    public Guid CustomerId { get; set; }

    [Column("stripe_subscription_id")]
    [Required]
    [MaxLength(255)]
    public string StripeSubscriptionId { get; set; } = string.Empty;

    [Column("stripe_price_id")]
    [Required]
    [MaxLength(255)]
    public string StripePriceId { get; set; } = string.Empty;

    [Column("stripe_product_id")]
    [Required]
    [MaxLength(255)]
    public string StripeProductId { get; set; } = string.Empty;

    [Column("plan_name")]
    [Required]
    [MaxLength(100)]
    public string PlanName { get; set; } = string.Empty; // solo, team, business, enterprise

    [Column("tier")]
    public SubscriptionTier Tier { get; set; } = SubscriptionTier.Solo;

    [Column("status")]
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty; // active, canceled, past_due, trialing, incomplete

    [Column("current_period_start")]
    public DateTime CurrentPeriodStart { get; set; }

    [Column("current_period_end")]
    public DateTime CurrentPeriodEnd { get; set; }

    [Column("trial_start")]
    public DateTime? TrialStart { get; set; }

    [Column("trial_end")]
    public DateTime? TrialEnd { get; set; }

    [Column("cancel_at_period_end")]
    public bool CancelAtPeriodEnd { get; set; } = false;

    [Column("canceled_at")]
    public DateTime? CanceledAt { get; set; }

    [Column("ended_at")]
    public DateTime? EndedAt { get; set; }

    [Column("amount", TypeName = "decimal(10, 2)")]
    public decimal Amount { get; set; }

    [Column("currency")]
    [MaxLength(3)]
    public string Currency { get; set; } = "usd";

    [Column("interval")]
    [MaxLength(20)]
    public string Interval { get; set; } = "month"; // month, year

    [Column("interval_count")]
    public int IntervalCount { get; set; } = 1;

    [Column("quantity")]
    public int Quantity { get; set; } = 1;

    [Column("max_users")]
    public int? MaxUsers { get; set; }

    [Column("max_properties")]
    public int? MaxProperties { get; set; }

    [Column("max_api_calls_per_month")]
    public int? MaxApiCallsPerMonth { get; set; }

    [Column("max_connections")]
    public int MaxConnections { get; set; } = 3; // Solo: 3, Team: 10, Business+: unlimited

    [Column("max_watches")]
    public int MaxWatches { get; set; } = 5; // Solo: 5, Team: 50, Business+: unlimited

    [Column("has_team_workspace")]
    public bool HasTeamWorkspace { get; set; } = false;

    [Column("has_pipelines")]
    public bool HasPipelines { get; set; } = false;

    [Column("has_ontology_designer")]
    public bool HasOntologyDesigner { get; set; } = false;

    [Column("has_webhooks")]
    public bool HasWebhooks { get; set; } = false;

    [Column("has_debug_view")]
    public bool HasDebugView { get; set; } = false;

    [Column("has_data_network")]
    public bool HasDataNetwork { get; set; } = false;

    [Column("has_prediction_sandbox")]
    public bool HasPredictionSandbox { get; set; } = false;

    [Column("has_collaboration_annotations")]
    public bool HasCollaborationAnnotations { get; set; } = false;

    [Column("features", TypeName = "jsonb")]
    public string? Features { get; set; }

    [Column("metadata", TypeName = "jsonb")]
    public string? Metadata { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    [ForeignKey("CustomerId")]
    public virtual Customer Customer { get; set; } = null!;
}

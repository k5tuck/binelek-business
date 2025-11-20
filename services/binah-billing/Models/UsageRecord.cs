using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Binah.Billing.Models;

[Table("usage_records")]
public class UsageRecord
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("subscription_id")]
    [Required]
    public Guid SubscriptionId { get; set; }

    [Column("tenant_id")]
    [Required]
    public Guid TenantId { get; set; }

    [Column("usage_type")]
    [Required]
    [MaxLength(100)]
    public string UsageType { get; set; } = string.Empty; // api_calls, users, properties, storage_gb

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Column("period_start")]
    public DateTime PeriodStart { get; set; }

    [Column("period_end")]
    public DateTime PeriodEnd { get; set; }

    [Column("metadata", TypeName = "jsonb")]
    public string? Metadata { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("SubscriptionId")]
    public virtual Subscription Subscription { get; set; } = null!;
}

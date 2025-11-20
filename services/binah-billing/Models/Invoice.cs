using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Binah.Billing.Models;

[Table("invoices")]
public class Invoice
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("customer_id")]
    [Required]
    public Guid CustomerId { get; set; }

    [Column("subscription_id")]
    public Guid? SubscriptionId { get; set; }

    [Column("stripe_invoice_id")]
    [Required]
    [MaxLength(255)]
    public string StripeInvoiceId { get; set; } = string.Empty;

    [Column("invoice_number")]
    [MaxLength(100)]
    public string? InvoiceNumber { get; set; }

    [Column("status")]
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty; // draft, open, paid, void, uncollectible

    [Column("amount_due", TypeName = "decimal(10, 2)")]
    public decimal AmountDue { get; set; }

    [Column("amount_paid", TypeName = "decimal(10, 2)")]
    public decimal AmountPaid { get; set; }

    [Column("amount_remaining", TypeName = "decimal(10, 2)")]
    public decimal AmountRemaining { get; set; }

    [Column("subtotal", TypeName = "decimal(10, 2)")]
    public decimal Subtotal { get; set; }

    [Column("tax", TypeName = "decimal(10, 2)")]
    public decimal? Tax { get; set; }

    [Column("total", TypeName = "decimal(10, 2)")]
    public decimal Total { get; set; }

    [Column("currency")]
    [MaxLength(3)]
    public string Currency { get; set; } = "usd";

    [Column("billing_reason")]
    [MaxLength(100)]
    public string? BillingReason { get; set; } // subscription_create, subscription_cycle, etc.

    [Column("period_start")]
    public DateTime? PeriodStart { get; set; }

    [Column("period_end")]
    public DateTime? PeriodEnd { get; set; }

    [Column("due_date")]
    public DateTime? DueDate { get; set; }

    [Column("paid_at")]
    public DateTime? PaidAt { get; set; }

    [Column("hosted_invoice_url")]
    [MaxLength(1000)]
    public string? HostedInvoiceUrl { get; set; }

    [Column("invoice_pdf_url")]
    [MaxLength(1000)]
    public string? InvoicePdfUrl { get; set; }

    [Column("line_items", TypeName = "jsonb")]
    public string? LineItems { get; set; }

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

    [ForeignKey("SubscriptionId")]
    public virtual Subscription? Subscription { get; set; }
}

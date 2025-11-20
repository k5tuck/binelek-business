using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Binah.Billing.Models;

[Table("payment_methods")]
public class PaymentMethod
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("customer_id")]
    [Required]
    public Guid CustomerId { get; set; }

    [Column("stripe_payment_method_id")]
    [Required]
    [MaxLength(255)]
    public string StripePaymentMethodId { get; set; } = string.Empty;

    [Column("type")]
    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty; // card, bank_account, etc.

    [Column("is_default")]
    public bool IsDefault { get; set; } = false;

    [Column("card_brand")]
    [MaxLength(50)]
    public string? CardBrand { get; set; } // visa, mastercard, amex, etc.

    [Column("card_last4")]
    [MaxLength(4)]
    public string? CardLast4 { get; set; }

    [Column("card_exp_month")]
    public int? CardExpMonth { get; set; }

    [Column("card_exp_year")]
    public int? CardExpYear { get; set; }

    [Column("billing_details", TypeName = "jsonb")]
    public string? BillingDetails { get; set; }

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

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Binah.Billing.Models;

[Table("customers")]
public class Customer
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("tenant_id")]
    [Required]
    public Guid TenantId { get; set; }

    [Column("stripe_customer_id")]
    [Required]
    [MaxLength(255)]
    public string StripeCustomerId { get; set; } = string.Empty;

    [Column("email")]
    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Column("name")]
    [MaxLength(255)]
    public string? Name { get; set; }

    [Column("phone")]
    [MaxLength(50)]
    public string? Phone { get; set; }

    [Column("address_line1")]
    [MaxLength(500)]
    public string? AddressLine1 { get; set; }

    [Column("address_line2")]
    [MaxLength(500)]
    public string? AddressLine2 { get; set; }

    [Column("city")]
    [MaxLength(100)]
    public string? City { get; set; }

    [Column("state")]
    [MaxLength(100)]
    public string? State { get; set; }

    [Column("postal_code")]
    [MaxLength(20)]
    public string? PostalCode { get; set; }

    [Column("country")]
    [MaxLength(2)]
    public string? Country { get; set; }

    [Column("metadata", TypeName = "jsonb")]
    public string? Metadata { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public virtual ICollection<PaymentMethod> PaymentMethods { get; set; } = new List<PaymentMethod>();
    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}

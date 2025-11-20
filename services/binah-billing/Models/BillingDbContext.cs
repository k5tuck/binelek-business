using Microsoft.EntityFrameworkCore;

namespace Binah.Billing.Models;

public class BillingDbContext : DbContext
{
    public BillingDbContext(DbContextOptions<BillingDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<Subscription> Subscriptions { get; set; } = null!;
    public DbSet<Invoice> Invoices { get; set; } = null!;
    public DbSet<PaymentMethod> PaymentMethods { get; set; } = null!;
    public DbSet<UsageRecord> UsageRecords { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Customer configuration
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.StripeCustomerId).IsUnique();
            entity.HasIndex(e => e.Email);
            entity.HasQueryFilter(e => e.DeletedAt == null);
        });

        // Subscription configuration
        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.StripeSubscriptionId).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.CustomerId, e.Status });
            entity.HasQueryFilter(e => e.DeletedAt == null);

            entity.HasOne(s => s.Customer)
                .WithMany(c => c.Subscriptions)
                .HasForeignKey(s => s.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Invoice configuration
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.SubscriptionId);
            entity.HasIndex(e => e.StripeInvoiceId).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasQueryFilter(e => e.DeletedAt == null);

            entity.HasOne(i => i.Customer)
                .WithMany(c => c.Invoices)
                .HasForeignKey(i => i.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(i => i.Subscription)
                .WithMany()
                .HasForeignKey(i => i.SubscriptionId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // PaymentMethod configuration
        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.StripePaymentMethodId).IsUnique();
            entity.HasIndex(e => new { e.CustomerId, e.IsDefault });
            entity.HasQueryFilter(e => e.DeletedAt == null);

            entity.HasOne(pm => pm.Customer)
                .WithMany(c => c.PaymentMethods)
                .HasForeignKey(pm => pm.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UsageRecord configuration
        modelBuilder.Entity<UsageRecord>(entity =>
        {
            entity.HasIndex(e => e.SubscriptionId);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.UsageType);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => new { e.TenantId, e.PeriodStart, e.PeriodEnd });

            entity.HasOne(ur => ur.Subscription)
                .WithMany()
                .HasForeignKey(ur => ur.SubscriptionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

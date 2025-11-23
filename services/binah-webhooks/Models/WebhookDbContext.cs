using Microsoft.EntityFrameworkCore;
using Binah.Webhooks.Models.Domain;
using System.Text.RegularExpressions;

namespace Binah.Webhooks.Models;

/// <summary>
/// Database context for webhooks service
/// </summary>
public class WebhookDbContext : DbContext
{
    public WebhookDbContext(DbContextOptions<WebhookDbContext> options) : base(options)
    {
    }

    public DbSet<WebhookSubscription> WebhookSubscriptions { get; set; } = null!;
    public DbSet<WebhookDelivery> WebhookDeliveries { get; set; } = null!;

    // GitHub Integration tables
    public DbSet<GitHubWebhookEvent> GitHubWebhookEvents { get; set; } = null!;
    public DbSet<GitHubOAuthToken> GitHubOAuthTokens { get; set; } = null!;
    public DbSet<AutonomousPullRequest> AutonomousPullRequests { get; set; } = null!;

    // Extension Marketplace tables
    public DbSet<Extension> Extensions { get; set; } = null!;
    public DbSet<InstalledExtension> InstalledExtensions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure PostgreSQL naming convention (snake_case)
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            // Convert table names to snake_case
            entity.SetTableName(entity.GetTableName()?.ToSnakeCase());

            // Convert column names to snake_case
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(property.GetColumnName().ToSnakeCase());
            }

            // Convert key names to snake_case
            foreach (var key in entity.GetKeys())
            {
                key.SetName(key.GetName()?.ToSnakeCase());
            }

            // Convert foreign key names to snake_case
            foreach (var foreignKey in entity.GetForeignKeys())
            {
                foreignKey.SetConstraintName(foreignKey.GetConstraintName()?.ToSnakeCase());
            }

            // Convert index names to snake_case
            foreach (var index in entity.GetIndexes())
            {
                index.SetDatabaseName(index.GetDatabaseName()?.ToSnakeCase());
            }
        }

        // WebhookSubscription configuration
        modelBuilder.Entity<WebhookSubscription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("webhook_subscriptions");
            entity.HasIndex(e => e.TenantId);

            entity.Property(e => e.TenantId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Url).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Events).HasColumnType("jsonb");
            entity.Property(e => e.Secret).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Headers).HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).IsRequired().HasDefaultValueSql("NOW()");
        });

        // WebhookDelivery configuration
        modelBuilder.Entity<WebhookDelivery>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("webhook_deliveries");
            entity.HasIndex(e => e.SubscriptionId);

            entity.Property(e => e.EventType).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Payload).HasColumnType("jsonb");
            entity.Property(e => e.ResponseStatus).HasMaxLength(50);
            entity.Property(e => e.DeliveredAt).IsRequired().HasDefaultValueSql("NOW()");
        });

        // GitHubWebhookEvent configuration
        modelBuilder.Entity<GitHubWebhookEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("github_webhook_events");
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.EventType);
            entity.HasIndex(e => e.ReceivedAt);

            entity.Property(e => e.EventType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.RepositoryName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Payload).IsRequired().HasColumnType("jsonb");
            entity.Property(e => e.Signature).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ReceivedAt).IsRequired().HasDefaultValueSql("NOW()");
            entity.Property(e => e.Processed).IsRequired().HasDefaultValue(false);
        });

        // GitHubOAuthToken configuration
        modelBuilder.Entity<GitHubOAuthToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("github_oauth_tokens");
            entity.HasIndex(e => e.TenantId).IsUnique();

            entity.Property(e => e.AccessToken).IsRequired().HasMaxLength(500);
            entity.Property(e => e.TokenType).IsRequired().HasMaxLength(50).HasDefaultValue("Bearer");
            entity.Property(e => e.Scope).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("NOW()");
            entity.Property(e => e.RefreshToken).HasMaxLength(500);
        });

        // AutonomousPullRequest configuration
        modelBuilder.Entity<AutonomousPullRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("autonomous_pull_requests");
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.Status);

            entity.Property(e => e.PrNumber).IsRequired();
            entity.Property(e => e.RepositoryName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.BranchName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.WorkflowType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50).HasDefaultValue("open");
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("NOW()");
        });

        // Extension configuration
        modelBuilder.Entity<Extension>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("extensions");
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.IsPublished);
            entity.HasIndex(e => e.IsOfficial);

            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.Author).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Version).IsRequired().HasMaxLength(50).HasDefaultValue("1.0.0");
            entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
            entity.Property(e => e.RequiredTier).IsRequired().HasMaxLength(50).HasDefaultValue("solo");
            entity.Property(e => e.InstallCount).IsRequired().HasDefaultValue(0);
            entity.Property(e => e.Rating).IsRequired().HasDefaultValue(0);
            entity.Property(e => e.RatingCount).IsRequired().HasDefaultValue(0);
            entity.Property(e => e.IconUrl).HasMaxLength(500);
            entity.Property(e => e.DefaultConfig).HasColumnType("jsonb").HasDefaultValue("{}");
            entity.Property(e => e.IsOfficial).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.IsPublished).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.Tags).HasColumnType("jsonb").HasDefaultValue("[]");
            entity.Property(e => e.DocumentationUrl).HasMaxLength(500);
            entity.Property(e => e.SupportUrl).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).IsRequired().HasDefaultValueSql("NOW()");
        });

        // InstalledExtension configuration
        modelBuilder.Entity<InstalledExtension>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("installed_extensions");
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.ExtensionId }).IsUnique();

            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.ExtensionId).IsRequired();
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50).HasDefaultValue("active");
            entity.Property(e => e.Config).HasColumnType("jsonb").HasDefaultValue("{}");
            entity.Property(e => e.InstalledVersion).IsRequired().HasMaxLength(50);
            entity.Property(e => e.InstalledAt).IsRequired().HasDefaultValueSql("NOW()");
            entity.Property(e => e.InstalledBy).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired().HasDefaultValueSql("NOW()");

            // Foreign key relationship to Extension
            entity.HasOne(e => e.Extension)
                .WithMany()
                .HasForeignKey(e => e.ExtensionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

/// <summary>
/// String extension methods for naming conventions
/// </summary>
internal static class StringExtensions
{
    /// <summary>
    /// Converts a PascalCase string to snake_case
    /// </summary>
    public static string ToSnakeCase(this string? input)
    {
        if (string.IsNullOrEmpty(input))
            return input ?? string.Empty;

        var startUnderscores = Regex.Match(input, @"^_+");
        return startUnderscores + Regex.Replace(input, @"([a-z0-9])([A-Z])", "$1_$2").ToLower();
    }
}

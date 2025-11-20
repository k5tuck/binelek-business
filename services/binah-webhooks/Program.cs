using Binah.Webhooks.Models;
using Binah.Webhooks.Models.Domain;
using Binah.Webhooks.Services;
using Binah.Webhooks.Services.Interfaces;
using Binah.Webhooks.Services.Implementations;
using Binah.Webhooks.Kafka;
using Binah.Webhooks.HealthChecks;
using Binah.Webhooks.Repositories.Interfaces;
using Binah.Webhooks.Repositories.Implementations;
using Binah.Core.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Prometheus;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger with JWT support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Binah Webhooks API",
        Version = "v1",
        Description = "Webhook management and delivery service for the Binah platform"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configure Database
builder.Services.AddDbContext<WebhookDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("WebhookDatabase")));

// Configure JWT
var jwtSettings = builder.Configuration.GetSection("JwtSettings");

var secret = jwtSettings.GetValue<string>("Secret") ?? throw new InvalidOperationException("JWT Secret is not configured");
var key = Encoding.UTF8.GetBytes(secret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // Set to true in production
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.GetValue<string>("Issuer"),
        ValidateAudience = true,
        ValidAudience = jwtSettings.GetValue<string>("Audience"),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Configure rate limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<AspNetCoreRateLimit.IpRateLimitOptions>(options =>
{
    options.EnableEndpointRateLimiting = true;
    options.StackBlockedRequests = false;
    options.HttpStatusCode = 429;
    options.RealIpHeader = "X-Real-IP";
    options.ClientIdHeader = "X-ClientId";
    options.GeneralRules = new List<AspNetCoreRateLimit.RateLimitRule>
    {
        new AspNetCoreRateLimit.RateLimitRule
        {
            Endpoint = "*",
            Period = "1m",
            Limit = 100 // 100 requests per minute per IP
        },
        new AspNetCoreRateLimit.RateLimitRule
        {
            Endpoint = "POST:/api/webhooks/*",
            Period = "1m",
            Limit = 50 // 50 POST requests per minute
        }
    };
});
builder.Services.AddSingleton<AspNetCoreRateLimit.IIpPolicyStore, AspNetCoreRateLimit.MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<AspNetCoreRateLimit.IRateLimitCounterStore, AspNetCoreRateLimit.MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<AspNetCoreRateLimit.IRateLimitConfiguration, AspNetCoreRateLimit.RateLimitConfiguration>();
builder.Services.AddSingleton<AspNetCoreRateLimit.IProcessingStrategy, AspNetCoreRateLimit.AsyncKeyLockProcessingStrategy>();

// Register HTTP context accessor for tenant context
builder.Services.AddHttpContextAccessor();

// Register tenant context service
builder.Services.AddScoped<ITenantContext, TenantContext>();

// Register URL validator for SSRF protection
builder.Services.AddSingleton<UrlValidator>();

// Register webhook services
builder.Services.AddScoped<IWebhookService, WebhookService>();
builder.Services.AddScoped<IWebhookDeliveryService, WebhookDeliveryService>();

// Register Kafka producer (Singleton - shared across all requests)
var kafkaBootstrapServers = builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
builder.Services.AddSingleton(new Binah.Infrastructure.Kafka.KafkaProducer(kafkaBootstrapServers));

// Register GitHub webhook service
builder.Services.AddScoped<IGitHubWebhookService, GitHubWebhookService>();

// Register GitHub event parser
builder.Services.AddScoped<IGitHubEventParser, GitHubEventParser>();

// Register GitHub event publisher for Kafka
builder.Services.AddScoped<IGitHubEventPublisher, GitHubEventPublisher>();

// Register GitHub integration repositories
builder.Services.AddScoped<IGitHubWebhookEventRepository, GitHubWebhookEventRepository>();
builder.Services.AddScoped<IGitHubOAuthTokenRepository, GitHubOAuthTokenRepository>();
builder.Services.AddScoped<IAutonomousPullRequestRepository, AutonomousPullRequestRepository>();

// Register GitHub API client and auth service
builder.Services.AddScoped<IGitHubApiClient, GitHubApiClient>();
builder.Services.AddScoped<IGitHubAuthService, GitHubAuthService>();

// Register GitHub rate limiter (Singleton - shared across all tenants)
builder.Services.AddSingleton<IGitHubRateLimiter, GitHubRateLimiter>();

// Register GitHub resilience policy (Scoped - per request)
builder.Services.AddScoped<IGitHubResiliencePolicy, GitHubResiliencePolicy>();

// Register GitHub branch and commit services
builder.Services.AddScoped<IGitHubBranchService, GitHubBranchService>();
builder.Services.AddScoped<IGitHubCommitService, GitHubCommitService>();

// Register GitHub pull request service (Sprint 2 Agent 3)
builder.Services.AddScoped<IGitHubPullRequestService, GitHubPullRequestService>();

// Register pull request template service
builder.Services.AddScoped<IPullRequestTemplateService, PullRequestTemplateService>();

// Register notification service (Sprint 3 Agent 4)
builder.Services.AddScoped<INotificationService, NotificationService>();

// Register autonomous PR service (Sprint 3 Agent 1)
builder.Services.AddScoped<IAutonomousPRService, AutonomousPRService>();

// Register extension marketplace services
builder.Services.AddScoped<IExtensionRepository, ExtensionRepository>();
builder.Services.AddScoped<IInstalledExtensionRepository, InstalledExtensionRepository>();
builder.Services.AddScoped<IExtensionService, ExtensionService>();

// Add HTTP client for webhook delivery
builder.Services.AddHttpClient("webhook", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Register Kafka consumer as hosted service
builder.Services.AddHostedService<KafkaEventConsumer>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure comprehensive health checks for Kubernetes
builder.Services.AddHealthChecks()
    // PostgreSQL health check
    .AddCheck("postgresql", () =>
    {
        try
        {
            var connectionString = builder.Configuration.GetConnectionString("WebhookDatabase")
                ?? "Host=localhost;Database=binah_webhooks;Username=binah;Password=binah";
            using var conn = new Npgsql.NpgsqlConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1";
            cmd.ExecuteScalar();
            return HealthCheckResult.Healthy("PostgreSQL is accessible");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"PostgreSQL unreachable: {ex.Message}");
        }
    }, tags: new[] { "db", "sql", "postgresql" })
    // Kafka consumer health check
    .AddCheck("kafka", () =>
    {
        try
        {
            // Simple check - just verify config is present
            var bootstrapServers = builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
            return string.IsNullOrEmpty(bootstrapServers)
                ? HealthCheckResult.Unhealthy("Kafka configuration missing")
                : HealthCheckResult.Healthy($"Kafka configured: {bootstrapServers}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Kafka check failed: {ex.Message}");
        }
    }, tags: new[] { "messaging", "kafka" })
    // Webhook delivery queue health check
    .AddCheck<WebhookQueueHealthCheck>(
        "webhook-delivery-queue",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "queue", "webhooks" })
    // Failed webhook retry queue health check
    .AddCheck<RetryQueueHealthCheck>(
        "retry-queue",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "queue", "webhooks", "retry" });

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Use custom middleware
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestTimingMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseCors("AllowAll");

// Add rate limiting middleware
app.UseMiddleware<AspNetCoreRateLimit.IpRateLimitMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// Add tenant context middleware AFTER authentication
app.UseMiddleware<Binah.Webhooks.Middleware.TenantContextMiddleware>();

app.MapControllers();

// Prometheus metrics
app.UseMetricServer();
app.UseHttpMetrics();

// Database migration
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WebhookDbContext>();
    try
    {
        db.Database.Migrate();
        Log.Information("Database migration completed successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while migrating the database");
    }
}

Log.Information("Starting Binah.Webhooks service on port 8099");

app.Run();

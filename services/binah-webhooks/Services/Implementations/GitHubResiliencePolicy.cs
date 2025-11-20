using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading.Tasks;
using Binah.Webhooks.Exceptions;
using Binah.Webhooks.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Octokit;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using Polly.Wrap;

namespace Binah.Webhooks.Services.Implementations;

/// <summary>
/// Implementation of GitHub API resilience policies using Polly
/// </summary>
public class GitHubResiliencePolicy : IGitHubResiliencePolicy
{
    private readonly ILogger<GitHubResiliencePolicy> _logger;
    private readonly IConfiguration _configuration;
    private readonly ConcurrentDictionary<string, AsyncPolicyWrap> _tenantPolicies;
    private readonly ConcurrentDictionary<string, AsyncCircuitBreakerPolicy> _circuitBreakers;

    private readonly int _retryCount;
    private readonly int _retryDelaySeconds;
    private readonly int _circuitBreakerFailureThreshold;
    private readonly int _circuitBreakerDurationSeconds;
    private readonly int _timeoutSeconds;
    private readonly int _maxConcurrentRequests;

    public GitHubResiliencePolicy(
        ILogger<GitHubResiliencePolicy> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _tenantPolicies = new ConcurrentDictionary<string, AsyncPolicyWrap>();
        _circuitBreakers = new ConcurrentDictionary<string, AsyncCircuitBreakerPolicy>();

        // Load configuration
        _retryCount = _configuration.GetValue<int>("GitHub:Resilience:RetryCount", 3);
        _retryDelaySeconds = _configuration.GetValue<int>("GitHub:Resilience:RetryDelaySeconds", 2);
        _circuitBreakerFailureThreshold = _configuration.GetValue<int>("GitHub:Resilience:CircuitBreakerFailureThreshold", 5);
        _circuitBreakerDurationSeconds = _configuration.GetValue<int>("GitHub:Resilience:CircuitBreakerDurationSeconds", 30);
        _timeoutSeconds = _configuration.GetValue<int>("GitHub:Resilience:TimeoutSeconds", 30);
        _maxConcurrentRequests = _configuration.GetValue<int>("GitHub:Resilience:MaxConcurrentRequests", 10);

        _logger.LogInformation(
            "GitHubResiliencePolicy initialized. Retry: {RetryCount}x{RetryDelay}s, Circuit breaker: {Threshold} failures/{Duration}s, Timeout: {Timeout}s, Max concurrent: {MaxConcurrent}",
            _retryCount,
            _retryDelaySeconds,
            _circuitBreakerFailureThreshold,
            _circuitBreakerDurationSeconds,
            _timeoutSeconds,
            _maxConcurrentRequests);
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, string tenantId)
    {
        var policy = GetOrCreatePolicy(tenantId);

        try
        {
            return await policy.ExecuteAsync(async () => await action());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute GitHub API call for tenant {TenantId} after all retries", tenantId);
            throw;
        }
    }

    public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action, string tenantId)
    {
        var retryPolicy = CreateRetryPolicy(tenantId);

        try
        {
            return await retryPolicy.ExecuteAsync(async () => await action());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute GitHub API call with retry for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task ExecuteAsync(Func<Task> action, string tenantId)
    {
        await ExecuteAsync(async () =>
        {
            await action();
            return true; // Dummy return value
        }, tenantId);
    }

    public string GetCircuitState(string tenantId)
    {
        if (_circuitBreakers.TryGetValue(tenantId, out var circuitBreaker))
        {
            return circuitBreaker.CircuitState.ToString();
        }

        return "NotInitialized";
    }

    public void ResetCircuit(string tenantId)
    {
        if (_circuitBreakers.TryGetValue(tenantId, out var circuitBreaker))
        {
            circuitBreaker.Reset();
            _logger.LogInformation("Circuit breaker reset for tenant {TenantId}", tenantId);
        }
        else
        {
            _logger.LogWarning("Attempted to reset non-existent circuit breaker for tenant {TenantId}", tenantId);
        }
    }

    private AsyncPolicyWrap GetOrCreatePolicy(string tenantId)
    {
        return _tenantPolicies.GetOrAdd(tenantId, _ => CreateCombinedPolicy(tenantId));
    }

    private AsyncPolicyWrap CreateCombinedPolicy(string tenantId)
    {
        var retryPolicy = CreateRetryPolicy(tenantId);
        var circuitBreakerPolicy = CreateCircuitBreakerPolicy(tenantId);
        var timeoutPolicy = CreateTimeoutPolicy(tenantId);
        var bulkheadPolicy = CreateBulkheadPolicy(tenantId);

        // Combine policies: Bulkhead → CircuitBreaker → Retry → Timeout
        return Policy.WrapAsync(bulkheadPolicy, circuitBreakerPolicy, retryPolicy, timeoutPolicy);
    }

    private AsyncRetryPolicy CreateRetryPolicy(string tenantId)
    {
        return Policy
            .Handle<HttpRequestException>()
            .Or<TimeoutRejectedException>()
            .Or<ApiException>(ex => IsTransientError(ex))
            .WaitAndRetryAsync(
                retryCount: _retryCount,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(_retryDelaySeconds * Math.Pow(2, retryAttempt - 1)), // Exponential backoff
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        exception,
                        "Retry {RetryCount}/{MaxRetries} for tenant {TenantId}. Waiting {Delay}s before next attempt",
                        retryCount,
                        _retryCount,
                        tenantId,
                        timeSpan.TotalSeconds);
                });
    }

    private AsyncCircuitBreakerPolicy CreateCircuitBreakerPolicy(string tenantId)
    {
        var policy = Policy
            .Handle<HttpRequestException>()
            .Or<ApiException>(ex => !IsRateLimitError(ex))
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: _circuitBreakerFailureThreshold,
                durationOfBreak: TimeSpan.FromSeconds(_circuitBreakerDurationSeconds),
                onBreak: (exception, duration) =>
                {
                    _logger.LogError(
                        exception,
                        "Circuit breaker opened for tenant {TenantId}. Duration: {Duration}s",
                        tenantId,
                        duration.TotalSeconds);
                },
                onReset: () =>
                {
                    _logger.LogInformation("Circuit breaker reset for tenant {TenantId}", tenantId);
                },
                onHalfOpen: () =>
                {
                    _logger.LogInformation("Circuit breaker half-open for tenant {TenantId}. Testing connection...", tenantId);
                });

        _circuitBreakers[tenantId] = policy;
        return policy;
    }

    private AsyncTimeoutPolicy CreateTimeoutPolicy(string tenantId)
    {
        return Policy
            .TimeoutAsync(
                timeout: TimeSpan.FromSeconds(_timeoutSeconds),
                timeoutStrategy: TimeoutStrategy.Pessimistic,
                onTimeoutAsync: (context, timeSpan, task) =>
                {
                    _logger.LogWarning(
                        "Request timeout after {Timeout}s for tenant {TenantId}",
                        timeSpan.TotalSeconds,
                        tenantId);
                    return Task.CompletedTask;
                });
    }

    private Polly.Bulkhead.AsyncBulkheadPolicy CreateBulkheadPolicy(string tenantId)
    {
        return Policy
            .BulkheadAsync(
                maxParallelization: _maxConcurrentRequests,
                maxQueuingActions: _maxConcurrentRequests * 2,
                onBulkheadRejectedAsync: context =>
                {
                    _logger.LogWarning(
                        "Bulkhead rejected request for tenant {TenantId}. Max concurrent requests ({MaxConcurrent}) exceeded",
                        tenantId,
                        _maxConcurrentRequests);
                    return Task.CompletedTask;
                });
    }

    private bool IsTransientError(ApiException ex)
    {
        // Retry on server errors (500, 502, 503, 504)
        var statusCode = (int)ex.StatusCode;
        return statusCode >= 500 && statusCode <= 599;
    }

    private bool IsRateLimitError(ApiException ex)
    {
        return ex.StatusCode == HttpStatusCode.Forbidden &&
               (ex.Message?.Contains("rate limit", StringComparison.OrdinalIgnoreCase) ?? false);
    }
}

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

namespace Binah.Webhooks.Services.Implementations;

/// <summary>
/// Implementation of GitHub API resilience policies using Polly v8
/// </summary>
public class GitHubResiliencePolicy : IGitHubResiliencePolicy
{
    private readonly ILogger<GitHubResiliencePolicy> _logger;
    private readonly IConfiguration _configuration;
    private readonly ConcurrentDictionary<string, ResiliencePipeline> _tenantPipelines;
    private readonly ConcurrentDictionary<string, CircuitBreakerStateProvider> _circuitBreakerStates;

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
        _tenantPipelines = new ConcurrentDictionary<string, ResiliencePipeline>();
        _circuitBreakerStates = new ConcurrentDictionary<string, CircuitBreakerStateProvider>();

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
        var pipeline = GetOrCreatePipeline(tenantId);

        try
        {
            return await pipeline.ExecuteAsync(async token => await action(), default);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute GitHub API call for tenant {TenantId} after all retries", tenantId);
            throw;
        }
    }

    public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action, string tenantId)
    {
        var pipeline = CreateRetryPipeline(tenantId);

        try
        {
            return await pipeline.ExecuteAsync(async token => await action(), default);
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
        if (_circuitBreakerStates.TryGetValue(tenantId, out var stateProvider))
        {
            return stateProvider.CircuitState.ToString();
        }

        return "NotInitialized";
    }

    public void ResetCircuit(string tenantId)
    {
        if (_circuitBreakerStates.TryGetValue(tenantId, out var stateProvider))
        {
            stateProvider.Isolate();
            stateProvider.Reset();
            _logger.LogInformation("Circuit breaker reset for tenant {TenantId}", tenantId);
        }
        else
        {
            _logger.LogWarning("Attempted to reset non-existent circuit breaker for tenant {TenantId}", tenantId);
        }
    }

    private ResiliencePipeline GetOrCreatePipeline(string tenantId)
    {
        return _tenantPipelines.GetOrAdd(tenantId, _ => CreateCombinedPipeline(tenantId));
    }

    private ResiliencePipeline CreateCombinedPipeline(string tenantId)
    {
        var stateProvider = new CircuitBreakerStateProvider();
        _circuitBreakerStates[tenantId] = stateProvider;

        return new ResiliencePipelineBuilder()
            // Timeout policy
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(_timeoutSeconds),
                OnTimeout = args =>
                {
                    _logger.LogWarning(
                        "Request timeout after {Timeout}s for tenant {TenantId}",
                        args.Timeout.TotalSeconds,
                        tenantId);
                    return ValueTask.CompletedTask;
                }
            })
            // Retry policy with exponential backoff
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = _retryCount,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromSeconds(_retryDelaySeconds),
                ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>()
                    .Handle<TimeoutRejectedException>()
                    .Handle<ApiException>(ex => IsTransientError(ex)),
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        args.Outcome.Exception,
                        "Retry {RetryCount}/{MaxRetries} for tenant {TenantId}. Waiting {Delay}s before next attempt",
                        args.AttemptNumber + 1,
                        _retryCount,
                        tenantId,
                        args.RetryDelay.TotalSeconds);
                    return ValueTask.CompletedTask;
                }
            })
            // Circuit breaker policy
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                MinimumThroughput = _circuitBreakerFailureThreshold,
                BreakDuration = TimeSpan.FromSeconds(_circuitBreakerDurationSeconds),
                ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>()
                    .Handle<ApiException>(ex => !IsRateLimitError(ex)),
                OnOpened = args =>
                {
                    _logger.LogError(
                        args.Outcome.Exception,
                        "Circuit breaker opened for tenant {TenantId}. Duration: {Duration}s",
                        tenantId,
                        args.BreakDuration.TotalSeconds);
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    _logger.LogInformation("Circuit breaker reset for tenant {TenantId}", tenantId);
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = args =>
                {
                    _logger.LogInformation("Circuit breaker half-open for tenant {TenantId}. Testing connection...", tenantId);
                    return ValueTask.CompletedTask;
                },
                StateProvider = stateProvider
            })
            .Build();
    }

    private ResiliencePipeline CreateRetryPipeline(string tenantId)
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = _retryCount,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromSeconds(_retryDelaySeconds),
                ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>()
                    .Handle<TimeoutRejectedException>()
                    .Handle<ApiException>(ex => IsTransientError(ex)),
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        args.Outcome.Exception,
                        "Retry {RetryCount}/{MaxRetries} for tenant {TenantId}. Waiting {Delay}s before next attempt",
                        args.AttemptNumber + 1,
                        _retryCount,
                        tenantId,
                        args.RetryDelay.TotalSeconds);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
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

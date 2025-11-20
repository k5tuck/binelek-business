using System;
using System.Threading.Tasks;

namespace Binah.Webhooks.Services.Interfaces;

/// <summary>
/// Service for applying resilience policies to GitHub API calls
/// Uses Polly library for retry, circuit breaker, timeout, and bulkhead patterns
/// </summary>
public interface IGitHubResiliencePolicy
{
    /// <summary>
    /// Execute an async action with all resilience policies applied
    /// (retry, circuit breaker, timeout, bulkhead)
    /// </summary>
    /// <typeparam name="T">Return type</typeparam>
    /// <param name="action">Action to execute</param>
    /// <param name="tenantId">Tenant ID for context</param>
    /// <returns>Result of the action</returns>
    Task<T> ExecuteAsync<T>(Func<Task<T>> action, string tenantId);

    /// <summary>
    /// Execute an async action with retry policy only
    /// Useful for operations where circuit breaker is not desired
    /// </summary>
    /// <typeparam name="T">Return type</typeparam>
    /// <param name="action">Action to execute</param>
    /// <param name="tenantId">Tenant ID for context</param>
    /// <returns>Result of the action</returns>
    Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action, string tenantId);

    /// <summary>
    /// Execute an async action without return value with all policies
    /// </summary>
    /// <param name="action">Action to execute</param>
    /// <param name="tenantId">Tenant ID for context</param>
    Task ExecuteAsync(Func<Task> action, string tenantId);

    /// <summary>
    /// Get circuit breaker state for a tenant
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <returns>Circuit state (Closed, Open, HalfOpen)</returns>
    string GetCircuitState(string tenantId);

    /// <summary>
    /// Reset circuit breaker for a tenant
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    void ResetCircuit(string tenantId);
}

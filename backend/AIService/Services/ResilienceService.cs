using Polly;
using Polly.CircuitBreaker;

namespace AIService.Services;

/// <summary>
/// Provides resilience policies (circuit breaker + retry) to handle transient failures
/// and prevent cascading outages when calling external APIs.
/// Uses Polly library for proven resilience patterns.
/// </summary>
public interface IResilienceService
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation);
    string GetStatus();
}

public class ResilienceService : IResilienceService
{
    private readonly IAsyncPolicy _policy;
    private readonly ILogger<ResilienceService> _logger;

    public ResilienceService(ILogger<ResilienceService> logger)
    {
        _logger = logger;

        // Retry policy: Exponential backoff (2s, 4s, 8s)
        var retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(new[]
            {
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(4),
                TimeSpan.FromSeconds(8)
            });

        // Circuit breaker: Opens after 5 failures, waits 30s before retrying
        var circuitBreakerPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<TimeoutException>()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));

        // Combine policies
        _policy = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);

        _logger.LogInformation(
            "ResilienceService initialized: Retry (3x) + CircuitBreaker (5 failures, 30s)"
        );
    }

    /// <summary>
    /// Executes an operation with resilience protection.
    /// Automatically retries on transient failures and applies circuit breaker logic.
    /// </summary>
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        try
        {
            return await _policy.ExecuteAsync(operation);
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex, "Circuit breaker is OPEN. Service temporarily unavailable.");
            throw new InvalidOperationException(
                "Service is currently unavailable due to repeated failures. Please try again in a moment.",
                ex
            );
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request timed out after exhausting all retries.");
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed after exhausting all retries.");
            throw;
        }
    }

    public string GetStatus() =>
        "✓ ResilienceService Active | Retry: 3x with exponential backoff | CircuitBreaker: 5 failures, 30s timeout";
}

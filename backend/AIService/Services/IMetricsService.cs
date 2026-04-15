namespace AIService.Services;

/// <summary>
/// Service for tracking and exposing Prometheus metrics
/// </summary>
public interface IMetricsService
{
    /// <summary>
    /// Record AI analysis completion
    /// </summary>
    void RecordAiAnalysis(string model, double durationMs, bool success, decimal costUsd);

    /// <summary>
    /// Record API request with response details
    /// </summary>
    void RecordApiRequest(string endpoint, string method, int statusCode, double durationMs);

    /// <summary>
    /// Record fallback usage with a reason label.
    /// </summary>
    void RecordFallbackUsage(string reason);

    /// <summary>
    /// Record circuit breaker state change
    /// </summary>
    void RecordCircuitBreakerStateChange(string state);

    /// <summary>
    /// Get total cost accumulated in USD
    /// </summary>
    decimal GetTotalCostUsd();

    /// <summary>
    /// Get total number of AI analyses performed
    /// </summary>
    long GetTotalAnalyses();

    /// <summary>
    /// Get Prometheus metrics in text format
    /// </summary>
    string GetMetricsText();
}

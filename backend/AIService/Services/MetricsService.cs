using System.Collections.Concurrent;
using System.Text;

namespace AIService.Services;

/// <summary>
/// Prometheus metrics service for tracking AI analysis and API performance
/// Simple implementation without external Prometheus dependency
/// </summary>
public class MetricsService : IMetricsService
{
    private decimal _totalCostUsd = 0;
    private long _totalAnalyses = 0;

    // Thread-safe counters for metrics
    private readonly ConcurrentDictionary<string, long> _analysisCounters = new();
    private readonly ConcurrentDictionary<string, List<double>> _responseTimes = new();
    private readonly ConcurrentDictionary<string, long> _apiRequestCounters = new();
    private readonly ConcurrentDictionary<string, long> _fallbackCounters = new();
    private readonly object _lockObj = new object();

    public MetricsService()
    {
        // Initialize
    }

    /// <summary>
    /// Record AI analysis completion
    /// </summary>
    public void RecordAiAnalysis(string model, double durationMs, bool success, decimal costUsd)
    {
        lock (_lockObj)
        {
            var key = $"{model}_{success}";
            _analysisCounters.AddOrUpdate(key, 1, (k, v) => v + 1);

            var responseTimeKey = model;
            _responseTimes.AddOrUpdate(responseTimeKey,
                new List<double> { durationMs / 1000.0 },
                (k, v) => { v.Add(durationMs / 1000.0); return v; });

            _totalCostUsd += costUsd;
            _totalAnalyses++;
        }
    }

    /// <summary>
    /// Record API request with response details
    /// </summary>
    public void RecordApiRequest(string endpoint, string method, int statusCode, double durationMs)
    {
        lock (_lockObj)
        {
            var key = $"{endpoint}_{method}_{statusCode}";
            _apiRequestCounters.AddOrUpdate(key, 1, (k, v) => v + 1);
        }
    }

    public void RecordFallbackUsage(string reason)
    {
        lock (_lockObj)
        {
            var key = string.IsNullOrWhiteSpace(reason) ? "unknown" : reason.Trim().ToLowerInvariant();
            _fallbackCounters.AddOrUpdate(key, 1, (_, v) => v + 1);
        }
    }

    /// <summary>
    /// Record circuit breaker state change
    /// </summary>
    public void RecordCircuitBreakerStateChange(string state)
    {
        // Just log it for now
    }

    /// <summary>
    /// Get total cost accumulated in USD
    /// </summary>
    public decimal GetTotalCostUsd() => _totalCostUsd;

    /// <summary>
    /// Get total number of AI analyses performed
    /// </summary>
    public long GetTotalAnalyses() => _totalAnalyses;

    /// <summary>
    /// Get Prometheus metrics in text format
    /// </summary>
    public string GetMetricsText()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# HELP ai_analysis_total Total number of AI analyses performed");
        sb.AppendLine("# TYPE ai_analysis_total counter");

        foreach (var kvp in _analysisCounters)
        {
            var parts = kvp.Key.Split('_');
            var model = parts[0];
            var success = parts.Length > 1 ? parts[1] : "false";
            sb.AppendLine($"ai_analysis_total{{model=\"{model}\",success=\"{success}\"}} {kvp.Value}");
        }

        sb.AppendLine("\n# HELP ai_response_duration_seconds AI response time in seconds");
        sb.AppendLine("# TYPE ai_response_duration_seconds histogram");

        foreach (var kvp in _responseTimes)
        {
            if (kvp.Value.Count > 0)
            {
                var avg = kvp.Value.Average();
                var max = kvp.Value.Max();
                var count = kvp.Value.Count;
                sb.AppendLine($"ai_response_duration_seconds_bucket{{le=\"0.1\",model=\"{kvp.Key}\"}} {(kvp.Value.Count(x => x <= 0.1))}");
                sb.AppendLine($"ai_response_duration_seconds_bucket{{le=\"+Inf\",model=\"{kvp.Key}\"}} {count}");
                sb.AppendLine($"ai_response_duration_seconds_sum{{model=\"{kvp.Key}\"}} {kvp.Value.Sum()}");
                sb.AppendLine($"ai_response_duration_seconds_count{{model=\"{kvp.Key}\"}} {count}");
            }
        }

        sb.AppendLine("\n# HELP ai_costs_usd_total Total cost of AI API calls in USD");
        sb.AppendLine("# TYPE ai_costs_usd_total gauge");
        sb.AppendLine($"ai_costs_usd_total {(double)_totalCostUsd:F4}");

        sb.AppendLine("\n# HELP api_requests_total Total number of API requests");
        sb.AppendLine("# TYPE api_requests_total counter");

        foreach (var kvp in _apiRequestCounters)
        {
            var parts = kvp.Key.Split('_');
            if (parts.Length >= 3)
            {
                var endpoint = parts[0];
                var method = parts[1];
                var status = parts[2];
                sb.AppendLine($"api_requests_total{{endpoint=\"{endpoint}\",method=\"{method}\",status=\"{status}\"}} {kvp.Value}");
            }
        }

        sb.AppendLine("\n# HELP ai_fallback_total Total number of fallback responses by reason");
        sb.AppendLine("# TYPE ai_fallback_total counter");

        foreach (var kvp in _fallbackCounters)
        {
            sb.AppendLine($"ai_fallback_total{{reason=\"{kvp.Key}\"}} {kvp.Value}");
        }

        return sb.ToString();
    }
}

namespace AIService.Events;

/// <summary>
/// Event published when AI analysis is completed
/// </summary>
public class SymptomAnalysisCompletedEvent
{
    public string CorrelationId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string Symptoms { get; set; } = string.Empty;
    public string Analysis { get; set; } = string.Empty;
    public string RecommendedSpecialty { get; set; } = string.Empty;
    public string UrgencyLevel { get; set; } = string.Empty;
    public decimal CostUsd { get; set; }
    public int TokensUsed { get; set; }
    public int ResponseTimeMs { get; set; }
    public string ModelUsed { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Event published when AI analysis fails and fallback is used
/// </summary>
public class SymptomAnalysisFallbackEvent
{
    public string CorrelationId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string Symptoms { get; set; } = string.Empty;
    public string FallbackAnalysis { get; set; } = string.Empty;
    public string FailureReason { get; set; } = string.Empty;
    public DateTime FailedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Event published when analysis is cached
/// </summary>
public class SymptomAnalysisCachedEvent
{
    public string CorrelationId { get; set; } = string.Empty;
    public string CacheKey { get; set; } = string.Empty;
    public string CachedAnalysis { get; set; } = string.Empty;
    public DateTime RetrievedAt { get; set; } = DateTime.UtcNow;
}

namespace AIService.Services;

public interface IFallbackService
{
    OpenAIResponse GetFallbackResponse(string symptoms, string correlationId);
    bool ShouldUseFallback(Exception ex);
}

public class FallbackService : IFallbackService
{
    private readonly ILogger<FallbackService> _logger;

    public FallbackService(ILogger<FallbackService> logger)
    {
        _logger = logger;
    }

    public OpenAIResponse GetFallbackResponse(string symptoms, string correlationId)
    {
        _logger.LogWarning("Using fallback response for symptoms analysis. CorrelationId: {CorrelationId}", correlationId);

        // Analyze symptoms locally with simple keyword matching
        var (conditions, urgency, specialty) = AnalyzeSymptomLocally(symptoms);

        var fallbackContent = BuildFallbackAnalysis(conditions, urgency, specialty);

        return new OpenAIResponse
        {
            IsSuccess = true,
            Content = fallbackContent,
            TokensUsed = 0,
            CostUsd = 0,
            ModelUsed = "fallback-local-analysis",
            ResponseTimeMs = 50,
            CorrelationId = correlationId
        };
    }

    public bool ShouldUseFallback(Exception ex)
    {
        // Use fallback for network errors, timeouts, and rate limiting
        return ex is HttpRequestException ||
               ex is TaskCanceledException ||
               ex is TimeoutException ||
               (ex.Message?.Contains("rate limit", StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private (List<string> conditions, string urgency, string specialty) AnalyzeSymptomLocally(string symptoms)
    {
        var lowerSymptoms = symptoms.ToLowerInvariant();
        var conditions = new List<string>();
        var urgency = "low";
        var specialty = "General Practice";

        // Chest symptoms - HIGH URGENCY
        if (lowerSymptoms.Contains("chest") || lowerSymptoms.Contains("heart"))
        {
            conditions.AddRange(new[] { "Angina", "Arrhythmia", "Myocardial Infarction" });
            urgency = "high";
            specialty = "Cardiology";
        }

        // Headache patterns
        if (lowerSymptoms.Contains("headache") || lowerSymptoms.Contains("migraine"))
        {
            if (lowerSymptoms.Contains("severe"))
            {
                conditions.AddRange(new[] { "Migraine", "Tension Headache", "Possible Meningitis" });
                urgency = "medium";
            }
            else
            {
                conditions.AddRange(new[] { "Tension Headache", "Fatigue-related" });
                urgency = "low";
            }
            specialty = "Neurology";
        }

        // Fever symptoms
        if (lowerSymptoms.Contains("fever") || lowerSymptoms.Contains("temperature"))
        {
            conditions.AddRange(new[] { "Viral Infection", "Bacterial Infection", "Influenza" });
            urgency = urgency == "high" ? "high" : "medium";
            specialty = "Infectious Disease";
        }

        // Respiratory symptoms
        if (lowerSymptoms.Contains("cough") || lowerSymptoms.Contains("throat") || lowerSymptoms.Contains("breath"))
        {
            conditions.AddRange(new[] { "Common Cold", "Bronchitis", "Pneumonia" });
            if (lowerSymptoms.Contains("severe"))
            {
                urgency = "medium";
            }
            specialty = "Pulmonology";
        }

        // Abdominal symptoms
        if (lowerSymptoms.Contains("stomach") || lowerSymptoms.Contains("abdominal") || lowerSymptoms.Contains("pain"))
        {
            conditions.AddRange(new[] { "Gastroenteritis", "Ulcer", "Appendicitis" });
            if (lowerSymptoms.Contains("severe"))
            {
                urgency = "high";
            }
            specialty = "Gastroenterology";
        }

        if (conditions.Count == 0)
        {
            conditions.Add("General Illness");
            specialty = "General Practice";
        }

        return (conditions, urgency, specialty);
    }

    private string BuildFallbackAnalysis(List<string> conditions, string urgency, string specialty)
    {
        var conditionsList = string.Join(", ", conditions);
        return $@"**OFFLINE ANALYSIS** (AI service temporarily unavailable)

**Possible Conditions:** {conditionsList}

**Urgency Level:** {urgency}

**Recommended Specialty:** {specialty}

**Disclaimer:** This is a basic local analysis generated when the AI service is unavailable.
It uses pattern matching and should NOT replace professional medical evaluation.
Please consult a healthcare professional for accurate diagnosis and treatment.

**Next Steps:**
- Schedule an appointment with a {specialty} specialist
- Monitor your symptoms
- Seek emergency care if symptoms worsen significantly";
    }
}

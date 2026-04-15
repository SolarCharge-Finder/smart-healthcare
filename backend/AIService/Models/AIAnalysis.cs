namespace AIService.Models;

public class AIAnalysis
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string SymptomsInput { get; set; } = string.Empty;
    public string AIResponse { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
    public string RecommendedSpecialty { get; set; } = string.Empty;
    public string UrgencyLevel { get; set; } = string.Empty;
    public List<string> PossibleConditions { get; set; } = new();
    public string Disclaimer { get; set; } = string.Empty;
    public int ApiTokensUsed { get; set; }
    public decimal CostUsd { get; set; }
    public string ModelUsed { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public string CorrelationId { get; set; } = string.Empty;

    // Feedback and Confirmation
    public bool FeedbackReceived { get; set; } = false;
    public bool? Confirmed { get; set; } // null = no feedback, true = confirmed correct, false = incorrect
}

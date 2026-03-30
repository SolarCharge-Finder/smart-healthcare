using AIService.Models;
using AIService.Services;
using FluentValidation;

namespace AIService.DTOs;

public class SymptomAnalysisRequest
{
    public string Symptoms { get; set; } = string.Empty;
    public Guid? PatientId { get; set; }
    public string? SessionToken { get; set; }

    public class Validator : FluentValidation.AbstractValidator<SymptomAnalysisRequest>
    {
        public Validator()
        {
            RuleFor(x => x.Symptoms)
                .NotEmpty().WithMessage("Symptoms description is required")
                .MinimumLength(10).WithMessage("Symptoms description must be at least 10 characters")
                .MaximumLength(2000).WithMessage("Symptoms description must not exceed 2000 characters")
                .Must(symptoms => !string.IsNullOrWhiteSpace(symptoms?.Trim())).WithMessage("Symptoms cannot be just whitespace");

            RuleFor(x => x.PatientId)
                .NotEmpty().When(x => x.PatientId.HasValue).WithMessage("Patient ID is required when provided");
        }
    }
}

public class SymptomAnalysisResponse
{
    public bool Success { get; set; }
    public string? Analysis { get; set; }
    public List<string>? PossibleConditions { get; set; }
    public double ConfidenceScore { get; set; }
    public string RecommendedSpecialty { get; set; } = string.Empty;
    public string UrgencyLevel { get; set; } = string.Empty;
    public string Disclaimer { get; set; } = string.Empty;
    public int TokensUsed { get; set; }
    public decimal CostUsd { get; set; }
    public string ModelUsed { get; set; } = string.Empty;
    public int ResponseTimeMs { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string? Error { get; set; }

    public static SymptomAnalysisResponse CreateSuccess(OpenAIResponse aiResponse, string correlationId)
    {
        return new SymptomAnalysisResponse
        {
            Success = true,
            Analysis = aiResponse.Content,
            TokensUsed = aiResponse.TokensUsed,
            CostUsd = aiResponse.CostUsd,
            ModelUsed = aiResponse.ModelUsed,
            ResponseTimeMs = aiResponse.ResponseTimeMs,
            CorrelationId = correlationId
        };
    }

    public static SymptomAnalysisResponse CreateError(string error, string correlationId)
    {
        return new SymptomAnalysisResponse
        {
            Success = false,
            Error = error,
            CorrelationId = correlationId,
            Disclaimer = "Analysis failed due to system error"
        };
    }
}

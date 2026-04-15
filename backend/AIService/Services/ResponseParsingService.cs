using System.Text.Json;
using AIService.Models;

namespace AIService.Services;

public interface IResponseParsingService
{
    ParsedAIResponse ParseAIResponse(string aiResponse, string correlationId);
}

public class ResponseParsingService : IResponseParsingService
{
    private readonly ILogger<ResponseParsingService> _logger;

    public ResponseParsingService(ILogger<ResponseParsingService> logger)
    {
        _logger = logger;
    }

    public ParsedAIResponse ParseAIResponse(string aiResponse, string correlationId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(aiResponse))
            {
                return new ParsedAIResponse
                {
                    IsValid = false,
                    ErrorMessage = "Empty AI response received",
                    CorrelationId = correlationId
                };
            }

            // Try to parse as JSON
            var jsonDoc = JsonDocument.Parse(aiResponse);

            if (!jsonDoc.RootElement.TryGetProperty("possibleConditions", out var conditionsElement))
            {
                return new ParsedAIResponse
                {
                    IsValid = false,
                    ErrorMessage = "Missing possibleConditions field in AI response",
                    CorrelationId = correlationId
                };
            }

            var conditions = new List<string>();
            if (conditionsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in conditionsElement.EnumerateArray())
                {
                    if (element.ValueKind == JsonValueKind.String)
                    {
                        var condition = NormalizeText(element.GetString());
                        if (!string.IsNullOrWhiteSpace(condition))
                        {
                            conditions.Add(condition);
                        }
                    }
                }
            }

            conditions = conditions
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(5)
                .ToList();

            if (conditions.Count == 0)
            {
                return new ParsedAIResponse
                {
                    IsValid = false,
                    ErrorMessage = "possibleConditions must contain at least one condition",
                    CorrelationId = correlationId
                };
            }

            if (!jsonDoc.RootElement.TryGetProperty("confidenceScore", out var confidenceElement) ||
                !confidenceElement.TryGetDouble(out var confidenceScore))
            {
                return new ParsedAIResponse
                {
                    IsValid = false,
                    ErrorMessage = "Missing or invalid confidenceScore field",
                    CorrelationId = correlationId
                };
            }

            if (!jsonDoc.RootElement.TryGetProperty("recommendedSpecialty", out var specialtyElement) ||
                specialtyElement.ValueKind != JsonValueKind.String)
            {
                return new ParsedAIResponse
                {
                    IsValid = false,
                    ErrorMessage = "Missing or invalid recommendedSpecialty field",
                    CorrelationId = correlationId
                };
            }

            if (!jsonDoc.RootElement.TryGetProperty("urgency", out var urgencyElement) ||
                urgencyElement.ValueKind != JsonValueKind.String)
            {
                return new ParsedAIResponse
                {
                    IsValid = false,
                    ErrorMessage = "Missing or invalid urgency field",
                    CorrelationId = correlationId
                };
            }

            // Validate confidence score range
            if (confidenceScore < 0 || confidenceScore > 1)
            {
                return new ParsedAIResponse
                {
                    IsValid = false,
                    ErrorMessage = "Confidence score must be between 0 and 1",
                    CorrelationId = correlationId
                };
            }

            // Validate urgency level
            var urgency = NormalizeUrgency(urgencyElement.GetString());
            var validUrgencyLevels = new[] { "Low", "Medium", "High", "Emergency" };
            if (!validUrgencyLevels.Contains(urgency))
            {
                return new ParsedAIResponse
                {
                    IsValid = false,
                    ErrorMessage = "Invalid urgency level. Must be: Low, Medium, High, or Emergency",
                    CorrelationId = correlationId
                };
            }

            // Validate specialty
            var specialty = NormalizeSpecialty(specialtyElement.GetString());
            if (string.IsNullOrWhiteSpace(specialty))
            {
                return new ParsedAIResponse
                {
                    IsValid = false,
                    ErrorMessage = "Recommended specialty cannot be empty",
                    CorrelationId = correlationId
                };
            }

            return new ParsedAIResponse
            {
                IsValid = true,
                PossibleConditions = conditions,
                ConfidenceScore = confidenceScore,
                RecommendedSpecialty = specialty,
                Urgency = urgency,
                Disclaimer = jsonDoc.RootElement.TryGetProperty("disclaimer", out var disclaimerElement) &&
                             disclaimerElement.ValueKind == JsonValueKind.String
                    ? NormalizeDisclaimer(disclaimerElement.GetString())
                    : "This is not medical advice. Please consult a healthcare professional.",
                CorrelationId = correlationId
            };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse AI response. CorrelationId: {CorrelationId}, Response: {Response}",
                correlationId, aiResponse);

            return new ParsedAIResponse
            {
                IsValid = false,
                ErrorMessage = "Invalid JSON format in AI response",
                CorrelationId = correlationId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error parsing AI response. CorrelationId: {CorrelationId}", correlationId);

            return new ParsedAIResponse
            {
                IsValid = false,
                ErrorMessage = "Unexpected error processing AI response",
                CorrelationId = correlationId
            };
        }
    }

    private static string NormalizeText(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        return string.Join(" ", input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string NormalizeSpecialty(string? specialty)
    {
        var normalized = NormalizeText(specialty);
        if (string.IsNullOrWhiteSpace(normalized))
            return string.Empty;

        return normalized;
    }

    private static string NormalizeDisclaimer(string? disclaimer)
    {
        var normalized = NormalizeText(disclaimer);
        return string.IsNullOrWhiteSpace(normalized)
            ? "This is not medical advice. Please consult a healthcare professional."
            : normalized;
    }

    private static string NormalizeUrgency(string? urgency)
    {
        var normalized = NormalizeText(urgency);
        return normalized.ToLowerInvariant() switch
        {
            "low" => "Low",
            "medium" => "Medium",
            "high" => "High",
            "emergency" => "Emergency",
            _ => normalized
        };
    }
}

public class ParsedAIResponse
{
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public List<string> PossibleConditions { get; set; } = new();
    public double ConfidenceScore { get; set; }
    public string RecommendedSpecialty { get; set; } = string.Empty;
    public string Urgency { get; set; } = string.Empty;
    public string Disclaimer { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}

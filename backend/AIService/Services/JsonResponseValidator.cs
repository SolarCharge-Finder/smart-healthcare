using System.Text.Json;
using AIService.DTOs;
using Serilog.Context;

namespace AIService.Services;

public interface IJsonResponseValidator
{
    (bool isValid, SymptomAnalysisResponse? parsedResponse, string? error) ValidateAndParseJson(string jsonString);
    bool IsValidJson(string input);
    SymptomAnalysisResponse CreateFallbackResponse(string error, string correlationId);
}

public class JsonResponseValidator : IJsonResponseValidator
{
    private readonly ILogger<JsonResponseValidator> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public JsonResponseValidator(ILogger<JsonResponseValidator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates and parses JSON response from OpenAI
    /// CRITICAL: This is the enforcement point for structured responses
    /// </summary>
    public (bool isValid, SymptomAnalysisResponse? parsedResponse, string? error) ValidateAndParseJson(string jsonString)
    {
        using (LogContext.PushProperty("ValidationStep", "JsonParsing"))
        {
            try
            {
                // Step 1: Check if valid JSON
                if (!IsValidJson(jsonString))
                {
                    var error = "Response is not valid JSON";
                    _logger.LogWarning("JSON Validation Failed: {Error}. Input: {Input}", error, jsonString);
                    return (false, null, error);
                }

                // Step 2: Parse as JsonDocument first to validate structure
                using (var doc = JsonDocument.Parse(jsonString, new JsonDocumentOptions { AllowTrailingCommas = false }))
                {
                    var root = doc.RootElement;

                    // Step 3: Validate required fields
                    var validationError = ValidateRequiredFields(root);
                    if (validationError != null)
                    {
                        _logger.LogWarning("Required Fields Validation Failed: {Error}", validationError);
                        return (false, null, validationError);
                    }

                    // Step 4: Validate field values (types, ranges)
                    var fieldError = ValidateFieldValues(root);
                    if (fieldError != null)
                    {
                        _logger.LogWarning("Field Values Validation Failed: {Error}", fieldError);
                        return (false, null, fieldError);
                    }
                }

                // Step 5: Deserialize to response object
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var response = JsonSerializer.Deserialize<SymptomAnalysisResponse>(jsonString, options);

                if (response == null)
                {
                    var error = "Failed to deserialize response to object";
                    _logger.LogWarning("Deserialization Failed: {Error}", error);
                    return (false, null, error);
                }

                _logger.LogInformation("JSON Validation Successful. Confidence: {Score}, Urgency: {Urgency}",
                    response.ConfidenceScore, response.Urgency);

                return (true, response, null);
            }
            catch (JsonException ex)
            {
                var error = $"JSON parsing exception: {ex.Message}";
                _logger.LogError(ex, "JSON Parsing Exception: {Error}", error);
                return (false, null, error);
            }
            catch (Exception ex)
            {
                var error = $"Unexpected error during validation: {ex.Message}";
                _logger.LogError(ex, "Unexpected Validation Error: {Error}", error);
                return (false, null, error);
            }
        }
    }

    /// <summary>
    /// Quick JSON syntax validation
    /// </summary>
    public bool IsValidJson(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var trimmed = input.Trim();
        if (!((trimmed.StartsWith('{') && trimmed.EndsWith('}')) ||
              (trimmed.StartsWith('[') && trimmed.EndsWith(']'))))
        {
            return false;
        }

        try
        {
            JsonDocument.Parse(input);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validate that all required fields exist
    /// </summary>
    private string? ValidateRequiredFields(JsonElement root)
    {
        string[] requiredFields = { "possibleConditions", "recommendedSpecialty", "urgency", "confidenceScore" };

        foreach (var field in requiredFields)
        {
            if (!root.TryGetProperty(field, out _))
            {
                return $"Missing required field: {field}";
            }
        }

        return null;
    }

    /// <summary>
    /// Validate field values match expected types and ranges
    /// </summary>
    private string? ValidateFieldValues(JsonElement root)
    {
        // Validate confidenceScore (0-1)
        if (root.TryGetProperty("confidenceScore", out var scoreElement))
        {
            if (scoreElement.ValueKind != JsonValueKind.Number)
                return "confidenceScore must be a number";

            if (!double.TryParse(scoreElement.GetRawText(), out var score) || score < 0 || score > 1)
                return "confidenceScore must be between 0 and 1";
        }

        // Validate urgency (enum: Low, Medium, High, Emergency)
        if (root.TryGetProperty("urgency", out var urgencyElement))
        {
            if (urgencyElement.ValueKind != JsonValueKind.String)
                return "urgency must be a string";

            var urgency = urgencyElement.GetString();
            if (!new[] { "Low", "Medium", "High", "Emergency" }.Contains(urgency))
                return $"urgency '{urgency}' is not valid. Must be: Low, Medium, High, Emergency";
        }

        // Validate possibleConditions is array
        if (root.TryGetProperty("possibleConditions", out var conditionsElement))
        {
            if (conditionsElement.ValueKind != JsonValueKind.Array)
                return "possibleConditions must be an array";

            // Validate each condition is either a plain string or object with name.
            foreach (var condition in conditionsElement.EnumerateArray())
            {
                if (condition.ValueKind == JsonValueKind.String)
                    continue;

                if (condition.ValueKind == JsonValueKind.Object && condition.TryGetProperty("name", out _))
                    continue;

                return "Each condition must be a string or an object with a 'name' field";
            }
        }

        // Validate recommendedSpecialty is string
        if (root.TryGetProperty("recommendedSpecialty", out var specialtyElement))
        {
            if (specialtyElement.ValueKind != JsonValueKind.String)
                return "recommendedSpecialty must be a string";

            var specialty = specialtyElement.GetString();
            if (string.IsNullOrWhiteSpace(specialty))
                return "recommendedSpecialty cannot be empty";
        }

        return null;
    }

    /// <summary>
    /// Create a safe fallback response when AI fails
    /// </summary>
    public SymptomAnalysisResponse CreateFallbackResponse(string error, string correlationId)
    {
        _logger.LogWarning("Creating fallback response due to: {Error}. CorrelationId: {CorrelationId}", error, correlationId);

        return new SymptomAnalysisResponse
        {
            Success = false,
            Error = error,
            Disclaimer = "⚠️ Unable to complete analysis. Please consult a healthcare professional.",
            ConfidenceScore = 0,
            RecommendedSpecialty = "General Practice",
            Urgency = "Medium",
            PossibleConditions = new List<string> { "Unable to determine - Please see a doctor" },
            CorrelationId = correlationId,
            Analysis = "Service temporarily unavailable. Please try again or contact support."
        };
    }
}

public class JsonValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
}

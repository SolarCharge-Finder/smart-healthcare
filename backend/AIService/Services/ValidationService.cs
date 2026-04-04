using System.Text.RegularExpressions;

namespace AIService.Services;

public interface IValidationService
{
    SymptomsValidationResult ValidateSymptoms(string symptoms);
    string SanitizeSymptoms(string symptoms);
}

public class ValidationService : IValidationService
{
    private static readonly string[] ForbiddenWords = {
        "suicide", "kill", "harm", "murder", "die", "death",
        "illegal", "drugs", "overdose", "poison", "weapon"
    };

    private static readonly string[] MedicalKeywords = {
        "pain", "ache", "fever", "cough", "headache", "nausea", "vomiting",
        "diarrhea", "fatigue", "weakness", "dizziness", "rash", "swelling",
        "shortness", "breath", "chest", "pressure", "infection", "inflammation"
    };

    public SymptomsValidationResult ValidateSymptoms(string symptoms)
    {
        var result = new SymptomsValidationResult { IsValid = true };

        if (string.IsNullOrWhiteSpace(symptoms))
        {
            result.IsValid = false;
            result.ErrorMessage = "Symptoms description is required";
            return result;
        }

        // Length validation
        if (symptoms.Length < 10)
        {
            result.IsValid = false;
            result.ErrorMessage = "Symptoms description must be at least 10 characters";
            return result;
        }

        if (symptoms.Length > 2000)
        {
            result.IsValid = false;
            result.ErrorMessage = "Symptoms description must not exceed 2000 characters";
            return result;
        }

        // Content validation
        var sanitizedSymptoms = symptoms.ToLowerInvariant();

        // Check for forbidden content
        foreach (var word in ForbiddenWords)
        {
            if (sanitizedSymptoms.Contains(word))
            {
                result.IsValid = false;
                result.ErrorMessage = "Symptoms description contains inappropriate content";
                return result;
            }
        }

        // Check for medical relevance
        var hasMedicalKeywords = MedicalKeywords.Any(keyword => sanitizedSymptoms.Contains(keyword));
        if (!hasMedicalKeywords)
        {
            result.IsValid = false;
            result.ErrorMessage = "Symptoms description must contain medical symptoms";
            return result;
        }

        // Pattern validation for common medical formats
        var validPatterns = new[]
        {
            @"(headache|migraine|fever|cough|nausea|vomiting|diarrhea|fatigue|weakness|dizziness|rash|swelling|pain|ache|sore|infection|inflammation)",
            @"\b(chest|abdominal|back|joint|muscle)\s+(pain|discomfort|tightness)",
            @"\b(shortness|difficulty)\s+(of\s+)?breathing"
        };

        var hasValidPattern = validPatterns.Any(pattern => Regex.IsMatch(sanitizedSymptoms, pattern, RegexOptions.IgnoreCase));
        if (!hasValidPattern)
        {
            result.IsValid = false;
            result.ErrorMessage = "Symptoms description must follow medical symptom format";
            return result;
        }

        result.SanitizedSymptoms = SanitizeSymptoms(symptoms);
        return result;
    }

    public string SanitizeSymptoms(string symptoms)
    {
        if (string.IsNullOrWhiteSpace(symptoms))
            return string.Empty;

        // Remove HTML tags
        var sanitized = Regex.Replace(symptoms, @"<[^>]*>", "", RegexOptions.IgnoreCase);

        // Remove excessive whitespace
        sanitized = Regex.Replace(sanitized, @"\s+", " ", RegexOptions.IgnoreCase);

        // Trim and return
        return sanitized.Trim();
    }
}

public class SymptomsValidationResult
{
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string SanitizedSymptoms { get; set; } = string.Empty;
}

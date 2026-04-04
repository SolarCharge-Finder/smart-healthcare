using Serilog.Context;

namespace AIService.Services;

public interface ISpecialtyMappingService
{
    string RecommendSpecialty(string symptoms, List<string> possibleConditions);
    double CalculateSpecialtyConfidence(string symptoms, string specialty);
    IEnumerable<(string specialty, double confidence)> GetTopSpecialties(string symptoms, int count = 3);
}

/// <summary>
/// Intelligent symptom-to-specialty mapping
/// CRITICAL: Ensures patients are routed to correct doctor
/// 
/// Strategy:
/// 1. Parse symptoms for medical keywords
/// 2. Map to probable specialties
/// 3. Calculate confidence scores
/// 4. Return ranked recommendations
/// </summary>
public class SpecialtyMappingService : ISpecialtyMappingService
{
    private readonly ILogger<SpecialtyMappingService> _logger;

    // Symptom-to-Specialty knowledge base
    private static readonly Dictionary<string, List<string>> SymptomSpecialtyMap = new()
    {
        // Cardiology
        {
            "chest pain",
            new() { "Cardiologist", "Pulmonologist", "Emergency Medicine" }
        },
        {
            "heart palpitations",
            new() { "Cardiologist", "Internist" }
        },
        {
            "shortness of breath",
            new() { "Pulmonologist", "Cardiologist", "Emergency Medicine" }
        },
        {
            "high blood pressure",
            new() { "Cardiologist", "Internist" }
        },

        // Respiratory
        {
            "persistent cough",
            new() { "Pulmonologist", "ENT", "Internist" }
        },
        {
            "wheezing",
            new() { "Pulmonologist", "Allergist" }
        },
        {
            "difficulty breathing",
            new() { "Pulmonologist", "Emergency Medicine" }
        },

        // Neurology
        {
            "headache",
            new() { "Neurologist", "Primary Care" }
        },
        {
            "severe headache",
            new() { "Neurologist", "Emergency Medicine" }
        },
        {
            "dizziness",
            new() { "Neurologist", "ENT", "Internist" }
        },
        {
            "migraine",
            new() { "Neurologist", "Primary Care" }
        },

        // Gastroenterology
        {
            "abdominal pain",
            new() { "Gastroenterologist", "Surgeon", "Emergency Medicine" }
        },
        {
            "nausea vomiting",
            new() { "Gastroenterologist", "Internist" }
        },
        {
            "diarrhea",
            new() { "Gastroenterologist", "Internist" }
        },
        {
            "heartburn",
            new() { "Gastroenterologist", "Primary Care" }
        },

        // Orthopedics
        {
            "joint pain",
            new() { "Orthopedist", "Rheumatologist", "Physical Medicine" }
        },
        {
            "back pain",
            new() { "Orthopedist", "Neurosurgeon", "Physical Medicine" }
        },
        {
            "muscle pain",
            new() { "Orthopedist", "Rheumatologist" }
        },

        // Dermatology
        {
            "rash",
            new() { "Dermatologist", "Allergist", "Internist" }
        },
        {
            "skin lesion",
            new() { "Dermatologist" }
        },
        {
            "itching",
            new() { "Dermatologist", "Allergist" }
        },

        // Infectious Diseases
        {
            "fever",
            new() { "Internist", "Infectious Disease", "Emergency Medicine" }
        },
        {
            "infection",
            new() { "Infectious Disease", "Internist" }
        },

        // Mental Health
        {
            "anxiety",
            new() { "Psychiatrist", "Psychologist", "Internist" }
        },
        {
            "depression",
            new() { "Psychiatrist", "Psychologist" }
        },

        // Endocrinology
        {
            "diabetes",
            new() { "Endocrinologist", "Internist" }
        },
        {
            "thyroid",
            new() { "Endocrinologist", "Internist" }
        }
    };

    // Condition-to-Specialty map
    private static readonly Dictionary<string, string> ConditionSpecialtyMap = new()
    {
        // Common conditions and their primary specialists
        { "Myocardial Infarction", "Cardiologist" },
        { "Heart Attack", "Cardiologist" },
        { "Angina", "Cardiologist" },
        { "Arrhythmia", "Cardiologist" },
        { "Hypertension", "Cardiologist" },
        
        { "Pneumonia", "Pulmonologist" },
        { "Asthma", "Pulmonologist" },
        { "COPD", "Pulmonologist" },
        { "Bronchitis", "Pulmonologist" },
        
        { "Migraine", "Neurologist" },
        { "Stroke", "Neurologist" },
        { "Seizure", "Neurologist" },
        { "Concussion", "Neurologist" },
        
        { "Gastritis", "Gastroenterologist" },
        { "Ulcer", "Gastroenterologist" },
        { "IBS", "Gastroenterologist" },
        
        { "Arthritis", "Orthopedist" },
        { "Fracture", "Orthopedist" },
        { "Herniated Disc", "Orthopedist" },
        
        { "COVID-19", "Internist" },
        { "Influenza", "Internist" },
        { "Common Cold", "Primary Care" },
    };

    public SpecialtyMappingService(ILogger<SpecialtyMappingService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Recommend primary specialty based on symptoms
    /// </summary>
    public string RecommendSpecialty(string symptoms, List<string> possibleConditions)
    {
        using (LogContext.PushProperty("SpecialtyMapping", "Recommendation"))
        {
            _logger.LogInformation("Mapping symptoms to specialty. Symptoms: {Symptoms}, Conditions: {Conditions}",
                symptoms, string.Join(", ", possibleConditions));

            // Step 1: Check conditions first (more reliable)
            foreach (var condition in possibleConditions)
            {
                if (ConditionSpecialtyMap.TryGetValue(condition, out var specialty))
                {
                    _logger.LogInformation("Matched condition '{Condition}' to specialty '{Specialty}'",
                        condition, specialty);
                    return specialty;
                }
            }

            // Step 2: Parse symptoms and get matching specialties
            var normalizedSymptoms = symptoms.ToLowerInvariant();
            var specialtyScores = new Dictionary<string, double>();

            foreach (var (symptomKey, specialties) in SymptomSpecialtyMap)
            {
                if (normalizedSymptoms.Contains(symptomKey))
                {
                    _logger.LogDebug("Found symptom keyword: '{Symptom}'", symptomKey);

                    // Add points to matching specialties (first specialty gets most points)
                    for (int i = 0; i < specialties.Count; i++)
                    {
                        var specialty = specialties[i];
                        var points = 1.0 / (i + 1); // Decreasing weights

                        if (!specialtyScores.ContainsKey(specialty))
                            specialtyScores[specialty] = 0;

                        specialtyScores[specialty] += points;
                    }
                }
            }

            // Step 3: Return highest scoring specialty
            if (specialtyScores.Count > 0)
            {
                var recommended = specialtyScores.OrderByDescending(x => x.Value).First().Key;
                _logger.LogInformation("Recommended specialty: '{Specialty}' with score {Score}",
                    recommended, specialtyScores[recommended]);
                return recommended;
            }

            // Fallback
            _logger.LogWarning("No specialty mapping found. Defaulting to General Practice");
            return "General Practice";
        }
    }

    /// <summary>
    /// Calculate confidence of specialty recommendation (0-1)
    /// </summary>
    public double CalculateSpecialtyConfidence(string symptoms, string specialty)
    {
        var normalizedSymptoms = symptoms.ToLowerInvariant();
        double matchScore = 0;
        int totalSymptomKeywords = 0;

        foreach (var (symptomKey, specialties) in SymptomSpecialtyMap)
        {
            if (normalizedSymptoms.Contains(symptomKey))
            {
                totalSymptomKeywords++;

                if (specialties.Contains(specialty))
                {
                    // More confidence if specialty is first choice
                    matchScore += specialties.IndexOf(specialty) == 0 ? 0.8 : 0.5;
                }
            }
        }

        if (totalSymptomKeywords == 0)
            return 0.5; // Neutral confidence if no keywords matched

        var confidence = Math.Min(1.0, matchScore / totalSymptomKeywords);
        
        _logger.LogDebug(
            "Calculated specialty confidence: {Specialty}={Confidence} " +
            "({MatchScore}/{TotalKeywords})",
            specialty, confidence, matchScore, totalSymptomKeywords);

        return confidence;
    }

    /// <summary>
    /// Get top N specialty recommendations with confidence scores
    /// </summary>
    public IEnumerable<(string specialty, double confidence)> GetTopSpecialties(
        string symptoms, int count = 3)
    {
        var normalizedSymptoms = symptoms.ToLowerInvariant();
        var specialtyScores = new Dictionary<string, double>();

        // Score all specialties
        foreach (var (symptomKey, specialties) in SymptomSpecialtyMap)
        {
            if (normalizedSymptoms.Contains(symptomKey))
            {
                for (int i = 0; i < specialties.Count; i++)
                {
                    var specialty = specialties[i];
                    var points = 1.0 / (i + 1);

                    if (!specialtyScores.ContainsKey(specialty))
                        specialtyScores[specialty] = 0;

                    specialtyScores[specialty] += points;
                }
            }
        }

        // Return top N sorted by score
        var topSpecialties = specialtyScores
            .OrderByDescending(x => x.Value)
            .Take(count)
            .Select(x => (specialty: x.Key, confidence: Math.Min(1.0, x.Value / 3)))
            .ToList();

        _logger.LogDebug("Top {Count} specialties: {Specialties}",
            count, string.Join(", ", topSpecialties.Select(s => $"{s.specialty}({s.confidence:P})")));

        return topSpecialties;
    }
}

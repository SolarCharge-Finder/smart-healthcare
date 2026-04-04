using Serilog.Context;

namespace AIService.Services;

public interface IUrgencyClassificationService
{
    (string urgencyLevel, string recommendation) ClassifyUrgency(string symptoms, List<string> possibleConditions);
    bool IsEmergencySituation(string symptoms, List<string> possibleConditions);
    string GetCarePathRecommendation(string urgencyLevel);
}

/// <summary>
/// Medical urgency classification engine
/// CRITICAL: Determines if patient needs emergency care vs routine appointment
/// 
/// Urgency Levels:
/// - Emergency: Immediate 911/hospital care required
/// - High: Urgent care within hours
/// - Medium: Doctor appointment within 24-48 hours
/// - Low: Routine appointment or self-care
/// </summary>
public class UrgencyClassificationService : IUrgencyClassificationService
{
    private readonly ILogger<UrgencyClassificationService> _logger;

    // Red flag symptoms that indicate emergency
    private static readonly string[] EmergencyKeywords = new[]
    {
        "chest pain", "difficulty breathing", "shortness of breath",
        "unconscious", "cannot speak", "sudden numbness",
        "severe bleeding", "choking", "unresponsive",
        "severe allergic reaction", "anaphylaxis",
        "suicidal thoughts", "self harm",
        "severe poisoning", "overdose",
        "trauma", "severe injury", "shock",
        "loss of consciousness", "seizure",
        "severe abdominal pain", "acute abdomen"
    };

    // High urgency symptoms (need urgent care same day)
    private static readonly string[] HighUrgencyKeywords = new[]
    {
        "severe pain", "high fever", "persistent vomiting",
        "severe headache", "sudden weakness", "vision changes",
        "confusion", "dehydration", "severe rash",
        "infection signs", "yellow skin", "dark urine",
        "difficulty swallowing", "severe cough"
    };

    // Conditions that require emergency/urgent care
    private static readonly Dictionary<string, string> ConditionUrgencyMap = new()
    {
        // Emergency (4)
        { "Myocardial Infarction", "Emergency" },
        { "Heart Attack", "Emergency" },
        { "Pulmonary Embolism", "Emergency" },
        { "Stroke", "Emergency" },
        { "Severe Pneumonia", "Emergency" },
        { "Meningitis", "Emergency" },
        { "Sepsis", "Emergency" },
        { "Anaphylaxis", "Emergency" },
        { "Acute Appendicitis", "Emergency" },
        { "Internal Bleeding", "Emergency" },

        // High Urgency (3)
        { "Pneumonia", "High" },
        { "Severe Bronchitis", "High" },
        { "Acute Asthma Attack", "High" },
        { "Kidney Stone", "High" },
        { "Severe Infection", "High" },
        { "Moderate Burn", "High" },
        { "Severe Allergic Reaction", "High" },

        // Medium Urgency (2)
        { "Mild Asthma", "Medium" },
        { "Bronchitis", "Medium" },
        { "Sinusitis", "Medium" },
        { "Ear Infection", "Medium" },
        { "Urinary Tract Infection", "Medium" },
        { "Gastritis", "Medium" },

        // Low Urgency (1)
        { "Common Cold", "Low" },
        { "Mild Headache", "Low" },
        { "Seasonal Allergy", "Low" },
        { "Mild Cough", "Low" },
    };

    public UrgencyClassificationService(ILogger<UrgencyClassificationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Classify urgency based on symptoms and conditions
    /// Uses multi-factor decision tree
    /// </summary>
    public (string urgencyLevel, string recommendation) ClassifyUrgency(
        string symptoms,
        List<string> possibleConditions)
    {
        using (LogContext.PushProperty("UrgencyClassification", "Processing"))
        {
            try
            {
                var normalizedSymptoms = symptoms.ToLowerInvariant();

                _logger.LogInformation(
                    "Classifying urgency. Symptoms: {SymptomCount} chars, Conditions: {ConditionCount}",
                    symptoms.Length, possibleConditions.Count);

                // Step 1: Check for emergency red flags
                if (IsEmergencySituation(symptoms, possibleConditions))
                {
                    _logger.LogWarning("EMERGENCY SITUATION DETECTED. Symptoms: {Symptoms}",
                        normalizedSymptoms.Substring(0, Math.Min(100, normalizedSymptoms.Length)));

                    return ("Emergency",
                        "🚨 EMERGENCY: Call 911 immediately. Seek immediate hospital care.");
                }

                // Step 2: Check condition-based urgency
                var maxConditionUrgency = GetMaxConditionUrgency(possibleConditions);

                // Step 3: Check symptom-based urgency
                var symptomUrgency = ClassifySymptomUrgency(normalizedSymptoms);

                // Step 4: Take the higher urgency level
                var urgencyLevel = SelectHigherUrgency(maxConditionUrgency, symptomUrgency);

                var recommendation = GetCarePathRecommendation(urgencyLevel);

                _logger.LogInformation(
                    "Urgency Classification Complete. " +
                    "Level: {Urgency}, ConditionBased: {ConditionUrgency}, " +
                    "SymptomBased: {SymptomUrgency}",
                    urgencyLevel, maxConditionUrgency, symptomUrgency);

                return (urgencyLevel, recommendation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error classifying urgency");
                // Default to Medium urgency on error (safe default)
                return ("Medium", GetCarePathRecommendation("Medium"));
            }
        }
    }

    /// <summary>
    /// Check if this is an emergency situation requiring immediate 911 call
    /// </summary>
    public bool IsEmergencySituation(string symptoms, List<string> possibleConditions)
    {
        var normalizedSymptoms = symptoms.ToLowerInvariant();

        // Check for emergency keywords
        foreach (var keyword in EmergencyKeywords)
        {
            if (normalizedSymptoms.Contains(keyword))
            {
                _logger.LogWarning("Emergency keyword detected: '{Keyword}'", keyword);
                return true;
            }
        }

        // Check for emergency conditions
        foreach (var condition in possibleConditions)
        {
            if (ConditionUrgencyMap.TryGetValue(condition, out var urgency) && urgency == "Emergency")
            {
                _logger.LogWarning("Emergency condition detected: '{Condition}'", condition);
                return true;
            }
        }

        // Check for multiple high-severity indicators
        var highSeverityCount = 0;
        if (normalizedSymptoms.Contains("chest pain") || normalizedSymptoms.Contains("chest"))
            highSeverityCount++;
        if (normalizedSymptoms.Contains("difficulty breathing") || normalizedSymptoms.Contains("shortness of breath"))
            highSeverityCount++;
        if (normalizedSymptoms.Contains("blood"))
            highSeverityCount++;
        if (normalizedSymptoms.Contains("unconscious") || normalizedSymptoms.Contains("unresponsive"))
            highSeverityCount++;

        if (highSeverityCount >= 2)
        {
            _logger.LogWarning("Multiple high-severity indicators detected: {Count}", highSeverityCount);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Get worst-case urgency from conditions list
    /// </summary>
    private string GetMaxConditionUrgency(List<string> conditions)
    {
        var urgencyOrder = new[] { "Emergency", "High", "Medium", "Low", "Unknown" };
        var maxUrgency = "Unknown";
        var maxIndex = urgencyOrder.Length - 1;

        foreach (var condition in conditions)
        {
            if (ConditionUrgencyMap.TryGetValue(condition, out var urgency))
            {
                var index = System.Array.IndexOf(urgencyOrder, urgency);
                if (index >= 0 && index < maxIndex)
                {
                    maxIndex = index;
                    maxUrgency = urgency;
                }
            }
        }

        _logger.LogDebug("Max Condition Urgency: {Urgency}", maxUrgency);
        return maxUrgency;
    }

    /// <summary>
    /// Classify urgency based on symptom keyword analysis
    /// </summary>
    private string ClassifySymptomUrgency(string normalizedSymptoms)
    {
        // Emergency keywords
        foreach (var keyword in EmergencyKeywords)
        {
            if (normalizedSymptoms.Contains(keyword))
            {
                _logger.LogDebug("Emergency symptom keyword found: '{Keyword}'", keyword);
                return "Emergency";
            }
        }

        // High urgency keywords
        foreach (var keyword in HighUrgencyKeywords)
        {
            if (normalizedSymptoms.Contains(keyword))
            {
                _logger.LogDebug("High urgency symptom keyword found: '{Keyword}'", keyword);
                return "High";
            }
        }

        // Check for fever + additional symptom
        if (normalizedSymptoms.Contains("fever"))
        {
            if (normalizedSymptoms.Contains("cough") || normalizedSymptoms.Contains("sore throat"))
                return "Medium";
        }

        // Check for combination of symptoms
        var symptomCount = 0;
        if (normalizedSymptoms.Contains("pain")) symptomCount++;
        if (normalizedSymptoms.Contains("nausea") || normalizedSymptoms.Contains("vomiting")) symptomCount++;
        if (normalizedSymptoms.Contains("fever")) symptomCount++;
        if (normalizedSymptoms.Contains("weakness")) symptomCount++;

        if (symptomCount >= 3)
            return "Medium";
        if (symptomCount >= 2)
            return "Low";

        _logger.LogDebug("Symptom urgency classified as: Low");
        return "Low";
    }

    /// <summary>
    /// Select the higher (more urgent) of two urgency levels
    /// </summary>
    private string SelectHigherUrgency(string urgency1, string urgency2)
    {
        var urgencyOrder = new Dictionary<string, int>
        {
            { "Emergency", 4 },
            { "High", 3 },
            { "Medium", 2 },
            { "Low", 1 },
            { "Unknown", 0 }
        };

        var level1 = urgencyOrder.GetValueOrDefault(urgency1, 0);
        var level2 = urgencyOrder.GetValueOrDefault(urgency2, 0);

        return level1 >= level2 ? urgency1 : urgency2;
    }

    /// <summary>
    /// Get care path recommendation based on urgency level
    /// </summary>
    public string GetCarePathRecommendation(string urgencyLevel)
    {
        return urgencyLevel switch
        {
            "Emergency" => "🚨 Call 911 or go to nearest Emergency Room immediately.",
            "High" => "⏰ Urgent care clinic or ER visit needed within 2-4 hours.",
            "Medium" => "📋 Schedule doctor appointment for today or next day.",
            "Low" => "✓ Monitor symptoms. Routine appointment within 1-2 weeks if symptoms persist.",
            _ => "📋 Consult with a healthcare provider."
        };
    }
}

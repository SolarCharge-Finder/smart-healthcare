using AIService.Models;

namespace AIService.Services;

public interface IPromptService
{
    string BuildMedicalAnalysisPrompt(string symptoms);
    string BuildSystemPrompt();
    string GetPromptVersion();
}

public class PromptService : IPromptService
{
    private const string CURRENT_VERSION = "1.0";

    public string BuildMedicalAnalysisPrompt(string symptoms)
    {
        var systemPrompt = BuildSystemPrompt();

        var userPrompt = $@"Analyze the following medical symptoms and provide a structured JSON response:
        
Symptoms: {symptoms}

Requirements:
1. Respond ONLY with valid JSON using this exact format:
{{
    ""possibleConditions"": [""condition1"", ""condition2"", ""condition3""],
    ""confidenceScore"": 0.00-1.00,
    ""recommendedSpecialty"": ""SpecialtyName"",
    ""urgency"": ""Low|Medium|High|Emergency"",
    ""disclaimer"": ""This is not medical advice. Consult a healthcare professional.""
}}

2. Identify the most likely medical conditions based on the symptoms
3. Provide a confidence score between 0.00 and 1.00
4. Recommend the most appropriate medical specialty
5. Assess urgency level (Low, Medium, High, Emergency)
6. Always include the disclaimer
7. Do not provide free text medical advice
8. Be conservative and prioritize safety

Common symptom patterns:
- Headache + fever + nausea: Possible migraine, flu
- Chest pain + shortness of breath: Possible cardiac issues
- Abdominal pain + vomiting: Possible gastrointestinal issues
- Cough + fever: Possible respiratory infection
- Rash + itching: Possible allergic reaction
- Dizziness + weakness: Possible neurological issues

Consider symptom severity, duration, and combinations when making your assessment.";

        return userPrompt;
    }

    public string BuildSystemPrompt()
    {
        return $@"You are a specialized medical AI symptom analyzer version {CURRENT_VERSION}. Your role is to analyze patient symptoms and provide structured, conservative assessments.

Core Principles:
1. Patient Safety First: Always err on the side of caution
2. Evidence-Based Analysis: Use established medical patterns
3. Structured Output Only: Never provide free-form medical advice
4. Clear Confidence Scoring: Be transparent about uncertainty
5. Appropriate Urgency Assessment: Consider all factors
6. Professional Communication: Use clear, medical terminology
7. Disclaimer Inclusion: Always include medical advice disclaimer

Output Requirements:
- Valid JSON only (no additional text, explanations, or conversational content)
- Complete all required fields: possibleConditions, confidenceScore, recommendedSpecialty, urgency, disclaimer
- Confidence scores must be realistic (0.3-0.9 for typical cases)
- Urgency levels: Low, Medium, High, Emergency
- Medical specialties must match established taxonomy
- Include standard medical disclaimer

Error Handling:
- If symptoms are insufficient, request clarification
- If symptoms indicate emergency, recommend immediate care
- If analysis is uncertain, use lower confidence scores
- Never diagnose specific conditions without sufficient information

You are processing real patient data - be professional, accurate, and safe.";
    }

    public string GetPromptVersion()
    {
        return CURRENT_VERSION;
    }
}

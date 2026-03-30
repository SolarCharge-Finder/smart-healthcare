namespace AIService.Services;

public static class PromptTemplates
{
    public const string MEDICAL_ANALYSIS = "medical_analysis";
    public const string EMERGENCY_ASSESSMENT = "emergency_assessment";
    public const string SYMPTOM_CLARIFICATION = "symptom_clarification";

    public static class EmergencyIndicators
    {
        public const string CHEST_PAIN = "chest pain";
        public const string SEVERE_BREATHING = "severe shortness of breath";
        public const string LOSS_OF_CONSCIOUSNESS = "loss of consciousness";
        public const string SUICIDAL_THOUGHTS = "suicidal thoughts";
        public const string SEVERE_BLEEDING = "severe bleeding";
        public const string STROKE_SYMPTOMS = "facial drooping, slurred speech, arm weakness";
    }

    public static class UrgencyKeywords
    {
        public static readonly Dictionary<string, string> LowUrgency = new Dictionary<string, string>
        {
            ["mild headache"] = "Low",
            ["minor fatigue"] = "Low",
            ["slight cough"] = "Low",
            ["occasional pain"] = "Low",
            ["mild fever"] = "Low"
        };

        public static readonly Dictionary<string, string> MediumUrgency = new Dictionary<string, string>
        {
            ["persistent headache"] = "Medium",
            ["moderate fever"] = "Medium",
            ["difficulty breathing"] = "Medium",
            ["significant pain"] = "Medium",
            ["reduced mobility"] = "Medium"
        };

        public static readonly Dictionary<string, string> HighUrgency = new Dictionary<string, string>
        {
            ["severe pain"] = "High",
            ["high fever"] = "High",
            ["inability to speak"] = "High",
            ["paralysis"] = "High",
            ["confusion"] = "High"
        };

        public static readonly Dictionary<string, string> EmergencyUrgency = new Dictionary<string, string>
        {
            [EmergencyIndicators.CHEST_PAIN] = "Emergency",
            [EmergencyIndicators.SEVERE_BREATHING] = "Emergency",
            [EmergencyIndicators.LOSS_OF_CONSCIOUSNESS] = "Emergency",
            [EmergencyIndicators.SUICIDAL_THOUGHTS] = "Emergency",
            [EmergencyIndicators.SEVERE_BLEEDING] = "Emergency",
            [EmergencyIndicators.STROKE_SYMPTOMS] = "Emergency"
        };
    }

    public static class SpecialtyMappings
    {
        public static readonly Dictionary<string, string> SymptomToSpecialty = new Dictionary<string, string>
        {
            ["headache"] = "Neurologist",
            ["migraine"] = "Neurologist",
            ["dizziness"] = "Neurologist",
            ["chest pain"] = "Cardiologist",
            ["shortness of breath"] = "Cardiologist",
            ["palpitations"] = "Cardiologist",
            ["rash"] = "Dermatologist",
            ["skin irritation"] = "Dermatologist",
            ["allergies"] = "Dermatologist",
            ["cough"] = "Pulmonologist",
            ["fever"] = "Pulmonologist",
            ["sore throat"] = "Pulmonologist",
            ["abdominal pain"] = "Gastroenterologist",
            ["nausea"] = "Gastroenterologist",
            ["vomiting"] = "Gastroenterologist",
            ["diarrhea"] = "Gastroenterologist",
            ["joint pain"] = "Orthopedic",
            ["muscle pain"] = "Orthopedic",
            ["stiffness"] = "Orthopedic",
            ["depression"] = "Psychiatrist",
            ["anxiety"] = "Psychiatrist",
            ["mood swings"] = "Psychiatrist",
            ["children symptoms"] = "Pediatrician",
            ["pediatric fever"] = "Pediatrician",
            ["pregnancy"] = "Gynecologist",
            ["maternal health"] = "Gynecologist",
            ["kidney problems"] = "Nephrologist",
            ["urinary issues"] = "Nephrologist",
            ["joint inflammation"] = "Rheumatologist",
            ["stiffness"] = "Rheumatologist",
            ["swelling"] = "Rheumatologist",
            ["infection"] = "InfectiousDisease",
            ["inflammation"] = "InfectiousDisease",
            ["lump"] = "Oncologist",
            ["tumor"] = "Oncologist",
            ["abnormal growth"] = "Oncologist",
            ["emergency"] = "EmergencyMedicine",
            ["trauma"] = "EmergencyMedicine",
            ["critical condition"] = "EmergencyMedicine"
        };
    }
}

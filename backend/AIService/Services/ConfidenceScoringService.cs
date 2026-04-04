using AIService.Data;
using AIService.Models;
using Serilog.Context;

namespace AIService.Services;

public interface IConfidenceScoringService
{
    double CalibrateConfidenceScore(
        string symptoms,
        List<string> possibleConditions,
        double rawAiConfidence,
        Guid? patientId);

    Task<double> AdjustScoreBasedOnFeedbackAsync(Guid analysisId, bool userConfirmed);
    
    double CalculateSymptomClarityScore(string symptoms);
    
    double CalculateConditionAgreementScore(List<string> conditions);
}

/// <summary>
/// Advanced confidence scoring with calibration and feedback adjustment
/// CRITICAL: Higher confidence score = more reliable AI recommendation
/// 
/// Score Components:
/// 1. Raw AI confidence (from OpenAI)
/// 2. Symptom clarity (how well symptoms are described)
/// 3. Condition agreement (do multiple conditions point to same specialty?)
/// 4. Historical accuracy (feedback adjustment)
/// 5. Evidence strength (how common is this condition?)
/// </summary>
public class ConfidenceScoringService : IConfidenceScoringService
{
    private readonly AiDbContext _dbContext;
    private readonly ILogger<ConfidenceScoringService> _logger;

    public ConfidenceScoringService(AiDbContext dbContext, ILogger<ConfidenceScoringService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Calibrate confidence score using multiple factors
    /// CRITICAL: Prevents over-confident or under-confident scores
    /// </summary>
    public double CalibrateConfidenceScore(
        string symptoms,
        List<string> possibleConditions,
        double rawAiConfidence,
        Guid? patientId)
    {
        using (LogContext.PushProperty("ConfidanceCalibration", "Processing"))
        {
            try
            {
                _logger.LogInformation(
                    "Starting confidence calibration. " +
                    "RawScore: {RawScore}, PatientId: {PatientId}, " +
                    "ConditionCount: {ConditionCount}",
                    rawAiConfidence, patientId, possibleConditions.Count);

                // Component 1: Base AI confidence (weight: 40%)
                var baseScore = rawAiConfidence * 0.40;
                _logger.LogDebug("Component 1 - Base AI Score: {Score}", baseScore);

                // Component 2: Symptom clarity (weight: 25%)
                var clarityScore = CalculateSymptomClarityScore(symptoms) * 0.25;
                _logger.LogDebug("Component 2 - Clarity Score: {Score}", clarityScore);

                // Component 3: Condition agreement (weight: 20%)
                var agreementScore = CalculateConditionAgreementScore(possibleConditions) * 0.20;
                _logger.LogDebug("Component 3 - Agreement Score: {Score}", agreementScore);

                // Component 4: Evidence strength (weight: 10%)
                var evidenceScore = CalculateEvidenceStrengthScore(possibleConditions) * 0.10;
                _logger.LogDebug("Component 4 - Evidence Score: {Score}", evidenceScore);

                // Component 5: Historical feedback (weight: 5%)
                double feedbackScore = 0;
                if (patientId.HasValue)
                {
                    feedbackScore = GetHistoricalAccuracyScore(patientId.Value) * 0.05;
                    _logger.LogDebug("Component 5 - Historical Score: {Score}", feedbackScore);
                }

                // Calculate final score
                var calibratedScore = baseScore + clarityScore + agreementScore + evidenceScore + feedbackScore;

                // Apply bounds (0-1)
                calibratedScore = Math.Max(0, Math.Min(1, calibratedScore));

                _logger.LogInformation(
                    "Confidence calibration complete. " +
                    "RawScore: {RawScore} → CalibratedScore: {CalibratedScore} " +
                    "(Change: {Change:+0.00;-0.00})",
                    rawAiConfidence, calibratedScore, calibratedScore - rawAiConfidence);

                return calibratedScore;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calibrating confidence score");
                return rawAiConfidence; // Return raw score if calibration fails
            }
        }
    }

    /// <summary>
    /// Adjust confidence score based on user feedback
    /// (e.g., user confirmed diagnosis was correct)
    /// </summary>
    public async Task<double> AdjustScoreBasedOnFeedbackAsync(Guid analysisId, bool userConfirmed)
    {
        try
        {
            // Find the analysis record
            var analysis = _dbContext.AIAnalyses.FirstOrDefault(a => a.Id == analysisId);
            
            if (analysis == null)
            {
                _logger.LogWarning("Analysis not found for feedback adjustment. Id: {Id}", analysisId);
                return 0;
            }

            var originalScore = analysis.ConfidenceScore;

            // Adjust score based on feedback
            if (userConfirmed)
            {
                // User confirmed - increase confidence slightly (max 95%)
                analysis.ConfidenceScore = Math.Min(0.95, analysis.ConfidenceScore + 0.05);
                _logger.LogInformation(
                    "Confidence score increased due to positive feedback. " +
                    "{Before} → {After}",
                    originalScore, analysis.ConfidenceScore);
            }
            else
            {
                // User denied - decrease confidence significantly
                analysis.ConfidenceScore = Math.Max(0, analysis.ConfidenceScore - 0.15);
                _logger.LogWarning(
                    "Confidence score decreased due to negative feedback. " +
                    "{Before} → {After}",
                    originalScore, analysis.ConfidenceScore);
            }

            analysis.FeedbackReceived = true;
            analysis.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            return analysis.ConfidenceScore;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adjusting confidence score based on feedback");
            return 0;
        }
    }

    /// <summary>
    /// Calculate clarity of symptom description (0-1)
    /// Clear symptoms = more detailed and specific
    /// </summary>
    public double CalculateSymptomClarityScore(string symptoms)
    {
        var normalizedSymptoms = symptoms.ToLowerInvariant();
        double clarityScore = 0;

        // Length factor: longer, more detailed descriptions = higher clarity
        var lengthScore = Math.Min(1.0, symptoms.Length / 500.0); // 500 chars = max clarity
        clarityScore += lengthScore * 0.3;

        // Word count factor: more words = more detail
        var wordCount = symptoms.Split(new[] { ' ', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
        var wordScore = Math.Min(1.0, wordCount / 50.0); // 50 words = max clarity
        clarityScore += wordScore * 0.3;

        // Specificity factor: presence of specific medical descriptors
        var specificKeywords = new[]
        {
            "sharp", "dull", "throbbing", "burning", "stabbing",
            "severe", "mild", "moderate", "constant", "intermittent",
            "radiating", "localized", "onset", "duration", "triggered"
        };

        var keywordCount = specificKeywords.Count(kw => normalizedSymptoms.Contains(kw));
        var specificityScore = Math.Min(1.0, keywordCount / 5.0); // 5+ keywords = max specificity
        clarityScore += specificityScore * 0.4;

        _logger.LogDebug(
            "Clarity Score: {Score} (Length: {Length}, Words: {Words}, Keywords: {Keywords})",
            clarityScore, symptoms.Length, wordCount, keywordCount);

        return Math.Min(1.0, clarityScore);
    }

    /// <summary>
    /// Calculate condition agreement score (0-1)
    /// If multiple conditions point to same specialty = higher agreement
    /// </summary>
    public double CalculateConditionAgreementScore(List<string> conditions)
    {
        if (conditions.Count <= 1)
            return 0.8; // Single condition is fairly confident

        // Group conditions by their primary specialty
        var specialtyGroups = new Dictionary<string, int>();

        var conditionSpecialtyMap = new Dictionary<string, string>
        {
            { "Myocardial Infarction", "Cardiology" },
            { "Heart Attack", "Cardiology" },
            { "Pneumonia", "Pulmonology" },
            { "Asthma", "Pulmonology" },
            { "Migraine", "Neurology" },
            // ... (additional mappings)
        };

        foreach (var condition in conditions)
        {
            if (conditionSpecialtyMap.TryGetValue(condition, out var specialty))
            {
                if (!specialtyGroups.ContainsKey(specialty))
                    specialtyGroups[specialty] = 0;
                specialtyGroups[specialty]++;
            }
        }

        // If all conditions map to same specialty = high agreement
        if (specialtyGroups.Count == 1)
            return 0.95;

        // If conditions split across specialties = lower agreement
        var agreementRatio = (double)specialtyGroups.Values.Max() / conditions.Count;

        _logger.LogDebug(
            "Condition Agreement Score: {Score} " +
            "({MaxInSpecialty}/{TotalConditions}, {SpecialtyCount} specialties)",
            agreementRatio, specialtyGroups.Values.Max(), conditions.Count, specialtyGroups.Count);

        return agreementRatio;
    }

    /// <summary>
    /// Calculate score based on how common/well-documented condition is
    /// </summary>
    private double CalculateEvidenceStrengthScore(List<string> conditions)
    {
        // Common conditions = stronger evidence base
        var commonConditions = new Dictionary<string, double>
        {
            { "Common Cold", 0.9 },
            { "Influenza", 0.85 },
            { "Migraine", 0.8 },
            { "Anxiety", 0.75 },
            { "Infection", 0.7 },
        };

        double totalScore = 0;
        foreach (var condition in conditions)
        {
            if (commonConditions.TryGetValue(condition, out var score))
                totalScore += score;
            else
                totalScore += 0.5; // Unknown conditions get moderate score
        }

        var averageEvidence = totalScore / conditions.Count;

        _logger.LogDebug("Evidence Strength Score: {Score}", averageEvidence);

        return averageEvidence;
    }

    /// <summary>
    /// Get patient's historical accuracy (based on previous feedback)
    /// </summary>
    private double GetHistoricalAccuracyScore(Guid patientId)
    {
        try
        {
            var patientAnalyses = _dbContext.AIAnalyses
                .Where(a => a.PatientId == patientId && a.FeedbackReceived)
                .ToList();

            if (patientAnalyses.Count == 0)
                return 0.5; // Neutral score if no history

            // Calculate ratio of confirmed analyses
            var confirmedCount = patientAnalyses.Count(a => a.Confirmed == true);
            var accuracy = (double)confirmedCount / patientAnalyses.Count;

            _logger.LogDebug(
                "Historical Accuracy for Patient {PatientId}: {Accuracy} " +
                "({Confirmed}/{Total} confirmed)",
                patientId, accuracy, confirmedCount, patientAnalyses.Count);

            return accuracy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating historical accuracy for patient {PatientId}", patientId);
            return 0.5;
        }
    }
}

using AIService.Models;
using Microsoft.EntityFrameworkCore;

namespace AIService.Data;

public static class AiDbContextSeed
{
    public static async Task SeedAsync(AiDbContext context)
    {
        await context.Database.EnsureCreatedAsync();
        
        // Check if data already exists
        if (await context.AIAnalyses.AnyAsync())
            return;

        // Sample seed data for testing
        var sampleAnalysis = new AIAnalysis
        {
            Id = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            SymptomsInput = "headache, fever, nausea",
            AIResponse = "{\"possibleConditions\":[\"migraine\",\"flu\"],\"confidenceScore\":0.85,\"recommendedSpecialty\":\"GeneralPractitioner\",\"urgency\":\"Medium\"}",
            ConfidenceScore = 0.85,
            RecommendedSpecialty = "GeneralPractitioner",
            UrgencyLevel = "Medium",
            PossibleConditions = new List<string> { "migraine", "flu" },
            Disclaimer = "This is not medical advice. Please consult a healthcare professional.",
            ApiTokensUsed = 150,
            CostUsd = 0.003m,
            ModelUsed = "gpt-4o",
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = DateTime.UtcNow,
            IsSuccessful = true,
            CorrelationId = Guid.NewGuid().ToString()
        };

        await context.AIAnalyses.AddAsync(sampleAnalysis);
        await context.SaveChangesAsync();
    }
}

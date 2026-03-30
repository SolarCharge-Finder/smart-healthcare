using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AIService.Services;

public interface IOpenAIService
{
    Task<OpenAIResponse> AnalyzeSymptomsAsync(string symptoms, string correlationId, CancellationToken cancellationToken = default);
}

public class OpenAIService : IOpenAIService
{
    private readonly IConfiguration _config;
    private readonly ILogger<OpenAIService> _logger;

    public OpenAIService(IConfiguration config, ILogger<OpenAIService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<OpenAIResponse> AnalyzeSymptomsAsync(string symptoms, string correlationId, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            _logger.LogInformation("Starting AI symptom analysis. CorrelationId: {CorrelationId}, Symptoms: {Symptoms}", 
                correlationId, symptoms);

            // Placeholder implementation - will be replaced with actual OpenAI integration in next commit
            await Task.Delay(100, cancellationToken); // Simulate API call

            var mockResponse = @"{
                ""possibleConditions"": [""migraine"", ""flu""],
                ""confidenceScore"": 0.85,
                ""recommendedSpecialty"": ""GeneralPractitioner"",
                ""urgency"": ""Medium"",
                ""disclaimer"": ""This is not medical advice. Consult a healthcare professional.""
            }";

            return new OpenAIResponse
            {
                IsSuccess = true,
                Content = mockResponse,
                TokensUsed = 150,
                ModelUsed = "gpt-4o",
                ResponseTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds,
                CorrelationId = correlationId,
                CostUsd = 0.003m
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AI service. CorrelationId: {CorrelationId}", correlationId);
            
            return new OpenAIResponse
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                CorrelationId = correlationId,
                ResponseTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
            };
        }
    }
}

public class OpenAIResponse
{
    public bool IsSuccess { get; set; }
    public string? Content { get; set; }
    public string? ErrorMessage { get; set; }
    public int TokensUsed { get; set; }
    public string? ModelUsed { get; set; }
    public int ResponseTimeMs { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public decimal CostUsd { get; set; }
}

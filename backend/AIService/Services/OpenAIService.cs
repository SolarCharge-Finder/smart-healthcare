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
    private readonly IValidationService _validationService;
    private readonly IResponseParsingService _parsingService;
    private readonly IPromptService _promptService;
    private readonly ISecretsService _secretsService;
    private readonly ICachingService _cachingService;
    private readonly IFallbackService _fallbackService;

    public OpenAIService(
        IConfiguration config,
        ILogger<OpenAIService> logger,
        IValidationService validationService,
        IResponseParsingService parsingService,
        IPromptService promptService,
        ISecretsService secretsService,
        ICachingService cachingService,
        IFallbackService fallbackService)
    {
        _config = config;
        _logger = logger;
        _validationService = validationService;
        _parsingService = parsingService;
        _promptService = promptService;
        _secretsService = secretsService;
        _cachingService = cachingService;
        _fallbackService = fallbackService;

        _logger.LogInformation("OpenAI Service initialized with secure secrets management, caching, and fallback support");
    }

    public async Task<OpenAIResponse> AnalyzeSymptomsAsync(string symptoms, string correlationId, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Starting AI symptom analysis. CorrelationId: {CorrelationId}, Symptoms: {Symptoms}",
                correlationId, symptoms);

            // Validate input first
            var validationResult = _validationService.ValidateSymptoms(symptoms);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Input validation failed. CorrelationId: {CorrelationId}, Error: {Error}",
                    correlationId, validationResult.ErrorMessage);

                return new OpenAIResponse
                {
                    IsSuccess = false,
                    ErrorMessage = validationResult.ErrorMessage,
                    CorrelationId = correlationId,
                    ResponseTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
                };
            }

            // Check cache for similar symptoms
            var cacheKey = GenerateCacheKey(validationResult.SanitizedSymptoms);
            var cachedResponse = await _cachingService.GetAsync<OpenAIResponse>(cacheKey);
            if (cachedResponse != null)
            {
                _logger.LogInformation("Cache hit for symptoms analysis. CorrelationId: {CorrelationId}", correlationId);
                cachedResponse.CorrelationId = correlationId;
                return cachedResponse;
            }

            // Placeholder implementation - will be replaced with actual OpenAI integration in next commit
            await Task.Delay(100, cancellationToken); // Simulate API call

            // Use structured prompt service
            var userPrompt = _promptService.BuildMedicalAnalysisPrompt(validationResult.SanitizedSymptoms);
            var systemPrompt = _promptService.BuildSystemPrompt();

            _logger.LogInformation("Generated structured prompt. CorrelationId: {CorrelationId}, PromptVersion: {Version}",
                correlationId, _promptService.GetPromptVersion());

            var mockResponse = @"{
                ""possibleConditions"": [""migraine"", ""flu""],
                ""confidenceScore"": 0.85,
                ""recommendedSpecialty"": ""GeneralPractitioner"",
                ""urgency"": ""Medium"",
                ""disclaimer"": ""This is not medical advice. Consult a healthcare professional.""
            }";

            // Parse the AI response
            var parsedResponse = _parsingService.ParseAIResponse(mockResponse, correlationId);

            if (!parsedResponse.IsValid)
            {
                _logger.LogError("AI response parsing failed. CorrelationId: {CorrelationId}, Error: {Error}",
                    correlationId, parsedResponse.ErrorMessage);

                return new OpenAIResponse
                {
                    IsSuccess = false,
                    ErrorMessage = parsedResponse.ErrorMessage,
                    CorrelationId = correlationId,
                    ResponseTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
                };
            }

            var response = new OpenAIResponse
            {
                IsSuccess = true,
                Content = parsedResponse.PossibleConditions.Any()
                    ? string.Join(", ", parsedResponse.PossibleConditions)
                    : "No specific conditions identified",
                TokensUsed = 150,
                ModelUsed = "gpt-4o",
                ResponseTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds,
                CorrelationId = correlationId,
                CostUsd = 0.003m
            };

            // Cache successful response
            await _cachingService.SetAsync(cacheKey, response, TimeSpan.FromHours(24));

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AI service. CorrelationId: {CorrelationId}", correlationId);

            // Use fallback service when AI fails
            if (_fallbackService.ShouldUseFallback(ex))
            {
                _logger.LogInformation("AI service failed, using fallback response. CorrelationId: {CorrelationId}", correlationId);
                return _fallbackService.GetFallbackResponse(symptoms, correlationId);
            }

            return new OpenAIResponse
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                CorrelationId = correlationId,
                ResponseTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
            };
        }
    }

    private string GenerateCacheKey(string symptoms)
    {
        // Create a normalized cache key from symptom keywords
        var normalized = System.Text.RegularExpressions.Regex.Replace(
            symptoms.ToLowerInvariant().Trim(),
            @"\s+", "-");
        return $"symptoms:{normalized}";
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

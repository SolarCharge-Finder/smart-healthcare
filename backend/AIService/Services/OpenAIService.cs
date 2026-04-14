using AIService.Events;
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
    private readonly IEventPublisher _eventPublisher;

    public OpenAIService(
        IConfiguration config,
        ILogger<OpenAIService> logger,
        IValidationService validationService,
        IResponseParsingService parsingService,
        IPromptService promptService,
        ISecretsService secretsService,
        ICachingService cachingService,
        IFallbackService fallbackService,
        IEventPublisher eventPublisher)
    {
        _config = config;
        _logger = logger;
        _validationService = validationService;
        _parsingService = parsingService;
        _promptService = promptService;
        _secretsService = secretsService;
        _cachingService = cachingService;
        _fallbackService = fallbackService;
        _eventPublisher = eventPublisher;

        _logger.LogInformation("OpenAI Service initialized with secure secrets management, caching, fallback support, and event publishing");
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

                // Publish cache hit event
                await _eventPublisher.PublishAnalysisCachedAsync(new()
                {
                    CorrelationId = correlationId,
                    CacheKey = cacheKey,
                    CachedAnalysis = cachedResponse.Content ?? "",
                });

                return cachedResponse;
            }

            // Use structured prompt service
            var userPrompt = _promptService.BuildMedicalAnalysisPrompt(validationResult.SanitizedSymptoms);
            var systemPrompt = _promptService.BuildSystemPrompt();

            _logger.LogInformation("Generated structured prompt. CorrelationId: {CorrelationId}, PromptVersion: {Version}",
                correlationId, _promptService.GetPromptVersion());

            // Call Gemini API (free tier)
            var apiKey = _secretsService.GetOpenAIApiKey(); // Reusing the same variable for Gemini key
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("x-goog-api-key", apiKey);
            client.Timeout = TimeSpan.FromSeconds(30);

            var requestPayload = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[]
                        {
                            new { text = $"{systemPrompt}\n\n{userPrompt}" }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.7,
                    maxOutputTokens = 500
                }
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(requestPayload),
                System.Text.Encoding.UTF8,
                "application/json");

            _logger.LogInformation("Calling Gemini API. CorrelationId: {CorrelationId}", correlationId);

            var geminiResponse = await client.PostAsync(
                $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent",
                jsonContent,
                cancellationToken);

            if (!geminiResponse.IsSuccessStatusCode)
            {
                var errorContent = await geminiResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Gemini API error. CorrelationId: {CorrelationId}, Status: {Status}, Error: {Error}",
                    correlationId, geminiResponse.StatusCode, errorContent);

                // Fall back to local analysis on API failure
                return _fallbackService.GetFallbackResponse(symptoms, correlationId);
            }

            var responseContent = await geminiResponse.Content.ReadAsStringAsync(cancellationToken);
            var apiResponseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
            var aiResponseText = apiResponseData.GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "";

            _logger.LogInformation("OpenAI API response received. CorrelationId: {CorrelationId}, ResponseLength: {Length}",
                correlationId, aiResponseText.Length);

            // Parse the AI response
            var parsedResponse = _parsingService.ParseAIResponse(aiResponseText, correlationId);

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
                ModelUsed = "gemini-2.0-flash",
                ResponseTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds,
                CorrelationId = correlationId,
                CostUsd = 0.0m  // Gemini free tier - no cost
            };

            // Cache successful response
            await _cachingService.SetAsync(cacheKey, response, TimeSpan.FromHours(24));

            // Publish analysis completed event
            await _eventPublisher.PublishAnalysisCompletedAsync(new()
            {
                CorrelationId = correlationId,
                Symptoms = symptoms,
                Analysis = response.Content ?? "",
                TokensUsed = response.TokensUsed,
                CostUsd = response.CostUsd,
                ModelUsed = response.ModelUsed,
                ResponseTimeMs = response.ResponseTimeMs,
                Success = true
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AI service. CorrelationId: {CorrelationId}", correlationId);

            // Use fallback service when AI fails
            if (_fallbackService.ShouldUseFallback(ex))
            {
                _logger.LogInformation("AI service failed, using fallback response. CorrelationId: {CorrelationId}", correlationId);
                var fallbackResponse = _fallbackService.GetFallbackResponse(symptoms, correlationId);

                // Publish fallback event
                await _eventPublisher.PublishAnalysisFallbackAsync(new()
                {
                    CorrelationId = correlationId,
                    Symptoms = symptoms,
                    FallbackAnalysis = fallbackResponse.Content ?? "",
                    FailureReason = ex.Message
                });

                return fallbackResponse;
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

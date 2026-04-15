using AIService.DTOs;
using AIService.Services;
using Microsoft.AspNetCore.Mvc;

namespace AIService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AiController : ControllerBase
{
    private readonly IOpenAIService _openAIService;
    private readonly IMetricsService _metricsService;
    private readonly ILogger<AiController> _logger;

    public AiController(IOpenAIService openAIService, IMetricsService metricsService, ILogger<AiController> logger)
    {
        _openAIService = openAIService;
        _metricsService = metricsService;
        _logger = logger;
    }

    [HttpGet("health")]
    public IActionResult GetHealth()
    {
        return Ok(new { status = "healthy", service = "ai-service", timestamp = DateTime.UtcNow });
    }

    [HttpPost("analyze")]
    public async Task<IActionResult> AnalyzeSymptoms([FromBody] SymptomAnalysisRequest request)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();

            _logger.LogInformation("Received symptom analysis request. Symptoms: {Symptoms}, CorrelationId: {CorrelationId}",
                request.Symptoms, correlationId);

            var result = await _openAIService.AnalyzeSymptomsAsync(request.Symptoms, correlationId);
            if (result == null)
            {
                var durationNull = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogError("AI service returned null response. CorrelationId: {CorrelationId}", correlationId);
                _metricsService.RecordAiAnalysis("unknown", durationNull, false, 0);
                _metricsService.RecordApiRequest("/api/ai/analyze", "POST", 500, durationNull);
                return StatusCode(500, SymptomAnalysisResponse.CreateError("AI service returned no response", correlationId));
            }

            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

            if (result.IsSuccess)
            {
                var isFallback = string.Equals(result.ModelUsed, "fallback-local-analysis", StringComparison.OrdinalIgnoreCase);

                // Record successful analysis metrics
                _metricsService.RecordAiAnalysis(result.ModelUsed ?? "unknown", duration, true, result.CostUsd);
                _metricsService.RecordApiRequest("/api/ai/analyze", "POST", 200, duration);
                if (isFallback)
                {
                    _metricsService.RecordFallbackUsage("fallback-service");
                    _logger.LogWarning("Fallback response returned. CorrelationId: {CorrelationId}", correlationId);
                }

                var response = SymptomAnalysisResponse.CreateSuccess(result, correlationId);
                return Ok(response);
            }
            else
            {
                // Record failed analysis metrics
                _metricsService.RecordAiAnalysis(result.ModelUsed ?? "unknown", duration, false, 0);
                _metricsService.RecordApiRequest("/api/ai/analyze", "POST", 500, duration);

                var response = SymptomAnalysisResponse.CreateError(result.ErrorMessage ?? "Analysis failed", correlationId);
                return StatusCode(500, response);
            }
        }
        catch (Exception ex)
        {
            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
            var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? "unknown";
            _logger.LogError(ex, "Error in symptom analysis endpoint. CorrelationId: {CorrelationId}", correlationId);

            // Record error metrics
            _metricsService.RecordAiAnalysis("unknown", duration, false, 0);
            _metricsService.RecordApiRequest("/api/ai/analyze", "POST", 500, duration);

            return StatusCode(500, new
            {
                success = false,
                error = "An unexpected error occurred during symptom analysis"
            });
        }
    }
}

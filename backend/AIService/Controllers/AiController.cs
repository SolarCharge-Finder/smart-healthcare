using AIService.DTOs;
using AIService.Services;
using Microsoft.AspNetCore.Mvc;

namespace AIService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AiController : ControllerBase
{
    private readonly IOpenAIService _openAIService;
    private readonly ILogger<AiController> _logger;

    public AiController(IOpenAIService openAIService, ILogger<AiController> logger)
    {
        _openAIService = openAIService;
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
        try
        {
            var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();
            
            _logger.LogInformation("Received symptom analysis request. Symptoms: {Symptoms}, CorrelationId: {CorrelationId}", 
                request.Symptoms, correlationId);

            var result = await _openAIService.AnalyzeSymptomsAsync(request.Symptoms, correlationId);

            if (result.IsSuccess)
            {
                var response = SymptomAnalysisResponse.CreateSuccess(result, correlationId);
                return Ok(response);
            }
            else
            {
                var response = SymptomAnalysisResponse.CreateError(result.ErrorMessage ?? "Analysis failed", correlationId);
                return StatusCode(500, response);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in symptom analysis endpoint");
            return StatusCode(500, new { 
                success = false,
                error = "An unexpected error occurred during symptom analysis"
            });
        }
    }
}

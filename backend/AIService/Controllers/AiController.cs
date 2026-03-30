using Microsoft.AspNetCore.Mvc;

namespace AIService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AiController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult GetHealth()
    {
        return Ok(new { status = "healthy", service = "ai-service", timestamp = DateTime.UtcNow });
    }

    [HttpPost("analyze")]
    public IActionResult AnalyzeSymptoms([FromBody] SymptomRequest request)
    {
        // Placeholder implementation - will be implemented in next commits
        return Ok(new { 
            message = "AI Service is ready for symptom analysis",
            symptoms = request.Symptoms,
            status = "placeholder"
        });
    }
}

public class SymptomRequest
{
    public string Symptoms { get; set; } = string.Empty;
}

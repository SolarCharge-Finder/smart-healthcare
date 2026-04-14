using Auth.Application.DTOs;
using Auth.Application.Interfaces;

using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;

namespace Auth.API.Controllers;

[ApiController]
[Route("internal/auth")]
public class InternalAuthController : ControllerBase
{
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _config;

    public InternalAuthController(ITokenService tokenService, IConfiguration config)
    {
        _tokenService = tokenService;
        _config = config;
    }

    [HttpPost("token")]
    public IActionResult GetToken(
        [FromHeader(Name = "x-api-key")] string apiKey,
        [FromBody] InternalTokenRequest request)
    {

        var expectedApiKey = _config["InternalAuth:ApiKey"] ?? throw new Exception("Internal API key not configured");

        // validate API key
        if (apiKey != expectedApiKey)
        {
            return Unauthorized();
        }

        // validate service name
        var allowedServices = new[] { "doctor-service", "patient-service", "admin-service" };

        if (!allowedServices.Contains(request.ServiceName))
        {
            return BadRequest("Invalid service name");
        }

        // generate token
        var token = _tokenService.GenerateInternalToken(request.ServiceName);

        return Ok(new { token });
    }
}

using Auth.Application.Interfaces;

using Microsoft.AspNetCore.Mvc;

namespace Auth.API.Controllers;

[ApiController]
[Route("internal/auth")]
public class InternalAuthController : ControllerBase
{
    private readonly ITokenService _tokenService;

    public InternalAuthController(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    [HttpPost("token")]
    public IActionResult GetToken()
    {
        var token = _tokenService.GenerateInternalToken("doctor-service");

        return Ok(new { token });
    }
}
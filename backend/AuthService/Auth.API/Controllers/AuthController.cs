namespace Auth.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using Auth.Application.Interfaces;
using Auth.Application.DTOs;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        await _authService.Register(request);
        return Ok();
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _authService.Login(request);

        if (user == null)
            return Unauthorized();

        return Ok(user);
    }
}
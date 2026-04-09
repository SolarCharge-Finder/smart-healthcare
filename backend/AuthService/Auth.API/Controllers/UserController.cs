using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Auth.Application.Interfaces;
using Auth.Application.DTOs;
using System.Security.Claims;

namespace Auth.API.Controllers;

[ApiController]
[Route("users")]
[Authorize] // protect all endpoints in this controller, user must be authenticated
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
                return Unauthorized();

            await _userService.UpdateName(Guid.Parse(userId), request.Name);

            return Ok(new { message = "Profile updated successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
                return Unauthorized();

            await _userService.ChangePassword(
                Guid.Parse(userId),
                request.CurrentPassword,
                request.NewPassword
            );

            return Ok(new { message = "Password changed successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
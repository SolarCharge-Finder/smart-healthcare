namespace UserService.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using UserService.Application.Interfaces;
using UserService.Application.DTOs;

[ApiController]
[Route("users")]
public class UserController : ControllerBase
{
    private readonly IUserService _service;

    public UserController(IUserService service)
    {
        _service = service;
    }

    // Get current logged-in user profile
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userId == null)
            return Unauthorized();

        var user = await _service.GetByUserId(Guid.Parse(userId));

        if (user == null)
            return NotFound();

        return Ok(user);
    }

    // Create profile (after register)
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(CreateUserRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
                return Unauthorized();

            var id = await _service.CreateUser(Guid.Parse(userId), request);

            return Ok(new { id });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Update own profile
    [Authorize]
    [HttpPut]
    public async Task<IActionResult> Update(UpdateUserRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
                return Unauthorized();

            await _service.UpdateUser(Guid.Parse(userId), request);

            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // User: deactivate own account
    [Authorize]
    [HttpPatch("me")]
    public async Task<IActionResult> DeactivateOwnAccount()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userId == null)
            return Unauthorized();

        await _service.DeactivateUser(Guid.Parse(userId));
        return Ok();
    }

    // Admin: get all users
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _service.GetAll();
        return Ok(users);
    }

    // Admin: get user by id
    [Authorize(Roles = "Admin")]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _service.GetById(id);

        if (user == null)
            return NotFound();

        return Ok(user);
    }

    // Admin: delete user
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _service.DeleteUser(id);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
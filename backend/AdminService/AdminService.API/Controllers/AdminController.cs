namespace AdminService.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using AdminService.Application.Interfaces;
using AdminService.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

[ApiController]
[Route("admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _service;

    public AdminController(IAdminService service)
    {
        _service = service;
    }

    // anyone logged in can request admin role
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(CreateAdminRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
                return Unauthorized();
                
            await _service.CreateAdmin(Guid.Parse(userId), request);
            
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // only approved admins can see all
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAll();
        return Ok(result);
    }

    // see pending admins for approval
    [Authorize(Roles = "Admin")]
    [HttpGet("pending")]
    public async Task<IActionResult> GetPending()
    {
        var result = await _service.GetPending();
        return Ok(result);
    }

    // approved admins can approve pending admins
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}/approve")]
    public async Task<IActionResult> ApproveAdmin(Guid id)
    {
        try
        {
            await _service.ApproveAdmin(id);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // approved admins can reject pending admins
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}/reject")]
    public async Task<IActionResult> RejectAdmin(Guid id)
    {
        try
        {
            await _service.RejectAdmin(id);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // get pending doctors for admin approval
    [HttpGet("doctors/pending")]
    public async Task<IActionResult> GetPendingDoctors()
    {
        var result = await _service.GetPendingDoctors();
        return Ok(result);
    }

    // approve doctor by admin
    [HttpPut("doctors/{id}/approve")]
    public async Task<IActionResult> ApproveDoctor(Guid id)
    {
        await _service.ApproveDoctor(id);
        return Ok();
    }
}
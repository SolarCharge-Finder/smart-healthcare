namespace Doctor.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using Doctor.Application.Interfaces;
using Doctor.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

[ApiController]
[Route("doctors")]
public class DoctorController : ControllerBase
{
    private readonly IDoctorService _service;

    public DoctorController(IDoctorService service)
    {
        _service = service;
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(CreateDoctorRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
                return Unauthorized();

            await _service.CreateDoctor(request, Guid.Parse(userId));
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var doctors = await _service.GetAll();
        return Ok(doctors);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("pending")]
    public async Task<IActionResult> GetPending()
    {
        var doctors = await _service.GetPending();
        return Ok(doctors);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}/approve")]
    public async Task<IActionResult> Approve(Guid id)
    {
        try
        {
            await _service.ApproveDoctor(id);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
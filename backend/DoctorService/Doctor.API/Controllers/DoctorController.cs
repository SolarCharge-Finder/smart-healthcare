namespace Doctor.API.Controllers;

using System.Security.Claims;

using Doctor.Application.DTOs;
using Doctor.Application.Interfaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
            {
                return Unauthorized();
            }

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

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMyDoctor()
    {
        // extract userId from JWT claims
        var userId = User.FindFirst("sub")?.Value;

        // fallback if you used NameIdentifier instead
        if (userId == null)
        {
            userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        if (userId == null)
        {
            return Unauthorized("User ID not found in token.");
        }

        // get doctor by userId
        var doctor = await _service.GetByUserId(Guid.Parse(userId));

        if (doctor == null)
        {
            return NotFound("Doctor profile not found.");
        }

        return Ok(doctor);
    }

    [HttpGet("approved")]
    public async Task<IActionResult> GetPublic()
    {
        var doctors = await _service.GetApproved();
        return Ok(doctors);
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchDoctors([FromQuery] SearchDoctorsRequest request)
    {
        var result = await _service.SearchDoctors(request);
        return Ok(result);
    }

    [HttpGet("filter-options")]
    public async Task<IActionResult> GetFilterOptions()
    {
        var result = await _service.GetFilterOptionsAsync();
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("pending")]
    public async Task<IActionResult> GetPending()
    {
        var doctors = await _service.GetPending();
        return Ok(doctors);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var doctor = await _service.GetById(id);

        if (doctor == null)
        {
            return NotFound();
        }

        return Ok(doctor);
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

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _service.DeleteDoctor(id);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

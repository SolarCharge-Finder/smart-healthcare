namespace PatientService.API.Controllers;

using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using PatientService.Application.DTOs;
using PatientService.Application.Interfaces;

[ApiController]
[Route("patient")]
public class PatientController : ControllerBase
{
    private readonly IPatientService _service;

    public PatientController(IPatientService service)
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
        {
            return Unauthorized();
        }

        var user = await _service.GetByUserId(Guid.Parse(userId));

        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    // Create profile (after register)
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(CreatePatientRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
            {
                return Unauthorized();
            }

            var id = await _service.CreatePatient(Guid.Parse(userId), request);

            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Update own profile
    [Authorize]
    [HttpPut]
    public async Task<IActionResult> Update(UpdatePatientRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
            {
                return Unauthorized();
            }

            await _service.UpdatePatient(Guid.Parse(userId), request);

            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Patient: deactivate own account
    [Authorize]
    [HttpPatch("me/deactivate")]
    public async Task<IActionResult> DeactivateOwnAccount()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
            {
                return Unauthorized();
            }

            await _service.DeactivatePatient(Guid.Parse(userId));
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Admin: get all patients
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var patients = await _service.GetAll();
        return Ok(patients);
    }

    // Admin: get patient by id
    [Authorize(Roles = "Admin")]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var patient = await _service.GetById(id);

        if (patient == null)
        {
            return NotFound();
        }

        return Ok(patient);
    }

    // Admin: delete patient
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _service.DeletePatient(id);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

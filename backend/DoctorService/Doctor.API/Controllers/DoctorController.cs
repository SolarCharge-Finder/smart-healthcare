namespace Doctor.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using Doctor.Application.Interfaces;
using Doctor.Application.DTOs;

[ApiController]
[Route("doctors")]
public class DoctorController : ControllerBase
{
    private readonly IDoctorService _service;

    public DoctorController(IDoctorService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateDoctorRequest request)
    {
        try
        {
            await _service.CreateDoctor(request);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var doctors = await _service.GetAll();
        return Ok(doctors);
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPending()
    {
        var doctors = await _service.GetPending();
        return Ok(doctors);
    }

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
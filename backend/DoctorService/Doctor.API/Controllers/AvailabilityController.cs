namespace Doctor.API.Controllers;

using Doctor.Application.DTOs;
using Doctor.Application.Interfaces;
using Doctor.Domain.Entities;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("doctors/{doctorId}/availability")]
public class DoctorAvailabilityController : ControllerBase
{
    private readonly IAvailabilityService _service;

    public DoctorAvailabilityController(IAvailabilityService service)
    {
        _service = service;
    }

    [Authorize(Policy = "DoctorOwner")]
    [HttpPost]
    public async Task<IActionResult> Create(
        Guid doctorId,
        [FromBody] CreateAvailability dto)
    {
        // Basic null/body validation
        if (dto == null)
        {
            return BadRequest("Request body is required");
        }

        // Time validation
        if (dto.StartTime >= dto.EndTime)
        {
            return BadRequest("StartTime must be earlier than EndTime");
        }

        // Recurring validation
        if (dto.IsRecurring && dto.DayOfWeek == null)
        {
            return BadRequest("DayOfWeek is required when IsRecurring is true");
        }

        if (!dto.IsRecurring && dto.DayOfWeek != null)
        {
            return BadRequest("DayOfWeek should be null when IsRecurring is false");
        }

        // Prevent past availability
        if (!dto.IsRecurring && dto.StartTime < DateTime.UtcNow)
        {
            return BadRequest("Cannot create availability in the past");
        }

        var startTime = DateTime.SpecifyKind(dto.StartTime, DateTimeKind.Utc); // ensure times are treated as UTC
        var endTime = DateTime.SpecifyKind(dto.EndTime, DateTimeKind.Utc);

        var availability = new DoctorAvailability
        {
            Id = Guid.NewGuid(),
            DoctorId = doctorId,
            Hospital = dto.Hospital?.Trim() ?? "",
            StartTime = startTime,
            EndTime = endTime,
            IsRecurring = dto.IsRecurring,
            DayOfWeek = dto.DayOfWeek,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            var result = await _service.CreateAsync(availability);
            return Ok(MapToResponse(result));
        }
        catch (Exception ex)
        {
            // might expand later to catch specific exceptions (e.g. validation, db errors) and return more specific status codes
            return BadRequest(ex.Message);
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(Guid doctorId)
    {
        if (doctorId == Guid.Empty)
        {
            return BadRequest("Invalid doctorId");
        }

        var result = await _service.GetByDoctorIdAsync(doctorId);

        return Ok(result.Select(MapToResponse));
    }

    [HttpGet("by-date")]
    public async Task<IActionResult> GetByDate(
        Guid doctorId,
        [FromQuery] DateTime date)
    {
        if (doctorId == Guid.Empty)
        {
            return BadRequest("Invalid doctorId");
        }

        if (date == default)
        {
            return BadRequest("Date query parameter is required");
        }

        var result = await _service.GetAvailabilityForDateAsync(doctorId, date);

        return Ok(result.Select(MapToResponse));
    }

    [Authorize(Policy = "DoctorOwner")]
    [HttpPatch("{availabilityId}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid availabilityId)
    {
        if (availabilityId == Guid.Empty)
        {
            return BadRequest("Invalid availabilityId");
        }

        try
        {
            await _service.DeactivateAsync(availabilityId);
            return NoContent();
        }
        catch (Exception ex)
        {
            return NotFound(ex.Message);
        }
    }

    // mapper 
    private static AvailabilityResponse MapToResponse(DoctorAvailability a)
    {
        return new AvailabilityResponse
        {
            Id = a.Id,
            DoctorId = a.DoctorId,
            Hospital = a.Hospital,
            StartTime = a.StartTime,
            EndTime = a.EndTime,
            IsActive = a.IsActive,
            IsRecurring = a.IsRecurring,
            DayOfWeek = a.DayOfWeek
        };
    }
}

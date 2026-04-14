namespace Doctor.Application.Services;

using Doctor.Application.DTOs;
using Doctor.Application.Interfaces;
using Doctor.Domain.Entities;

public class AvailabilityService : IAvailabilityService
{
    private readonly IAvailabilityRepository _repo;

    public AvailabilityService(IAvailabilityRepository repo)
    {
        _repo = repo;
    }

    public async Task<DoctorAvailability> CreateAsync(DoctorAvailability availability)
    {
        var existing = await _repo.GetByDoctorIdAsync(availability.DoctorId);

        var overlap = existing.Any(a =>
            a.IsActive &&
            (
                // New slot overlaps existing
                (availability.StartTime < a.EndTime && availability.EndTime > a.StartTime)

                ||

                // Recurring overlap (if both are recurring and on same day)
                (availability.IsRecurring && a.IsRecurring && availability.DayOfWeek == a.DayOfWeek)
            )
        );

        if (availability.StartTime.Kind != DateTimeKind.Utc || availability.EndTime.Kind != DateTimeKind.Utc)
        {
            throw new Exception("StartTime and EndTime must be in UTC");
        }

        if (availability.StartTime >= availability.EndTime)
        {
            throw new Exception("Invalid time range");
        }

        if (overlap)
        {
            throw new Exception("Availability overlaps with existing slot");
        }

        return await _repo.AddAsync(availability);
    }

    public async Task<List<DoctorAvailability>> GetByDoctorIdAsync(Guid doctorId)
    {
        return await _repo.GetByDoctorIdAsync(doctorId);
    }

    public async Task<List<DoctorAvailability>> GetAvailabilityForDateAsync(Guid doctorId, DateTime date)
    {
        var all = await _repo.GetByDoctorIdAsync(doctorId);

        return all.Where(a =>
            a.IsActive &&
            (
                // One-time availability
                (!a.IsRecurring && a.StartTime.Date == date.Date)

                ||

                // Recurring availability
                (a.IsRecurring && a.DayOfWeek == date.DayOfWeek)
            )
        ).ToList();
    }

    public async Task DeactivateAsync(Guid availabilityId)
    {
        var availability = await _repo.GetByIdAsync(availabilityId);

        if (availability == null)
        {
            throw new Exception("Availability not found");
        }

        availability.IsActive = false;

        await _repo.UpdateAsync(availability);
    }
}

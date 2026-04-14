namespace Doctor.Application.Interfaces;

using Doctor.Domain.Entities;

public interface IAvailabilityService
{
    Task<DoctorAvailability> CreateAsync(DoctorAvailability availability);

    Task<List<DoctorAvailability>> GetByDoctorIdAsync(Guid doctorId);

    Task<List<DoctorAvailability>> GetAvailabilityForDateAsync(Guid doctorId, DateTime date);

    Task DeactivateAsync(Guid availabilityId);
}

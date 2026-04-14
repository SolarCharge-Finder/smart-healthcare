namespace Doctor.Application.Interfaces;

using Doctor.Domain.Entities;

public interface IAvailabilityRepository
{
    Task<DoctorAvailability> AddAsync(DoctorAvailability availability);

    Task<List<DoctorAvailability>> GetByDoctorIdAsync(Guid doctorId);

    Task<DoctorAvailability?> GetByIdAsync(Guid id);

    Task UpdateAsync(DoctorAvailability availability);
}

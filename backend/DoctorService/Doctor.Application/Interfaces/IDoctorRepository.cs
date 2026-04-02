namespace Doctor.Application.Interfaces;

using Doctor.Domain.Entities;

public interface IDoctorRepository
{
    Task AddAsync(Doctor doctor);

    Task<Doctor?> GetByIdAsync(Guid id);

    Task<Doctor?> GetByUserIdAsync(Guid userId);

    Task<List<Doctor>> GetApprovedAsync();

    Task<List<Doctor>> GetAllAsync();

    Task<List<Doctor>> GetPendingAsync();

    Task RemoveAsync(Doctor doctor);

    Task SaveChangesAsync();
}
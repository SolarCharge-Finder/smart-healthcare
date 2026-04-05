namespace PatientService.Application.Interfaces;

using PatientService.Domain.Entities;

public interface IPatientRepository
{
    Task AddAsync(Patient patient);

    Task<Patient?> GetByIdAsync(Guid id);

    Task<Patient?> GetByUserIdAsync(Guid userId);

    Task<List<Patient>> GetAllAsync();

    Task RemoveAsync(Patient patient);

    Task SaveChangesAsync();
}
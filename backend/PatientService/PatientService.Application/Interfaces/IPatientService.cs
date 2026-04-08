namespace PatientService.Application.Interfaces;

using PatientService.Application.DTOs;

public interface IPatientService
{
    Task<Guid> CreatePatient(Guid userId, CreatePatientRequest request);

    Task<PatientResponse?> GetByUserId(Guid userId);

    Task<PatientResponse?> GetById(Guid id);

    Task<List<PatientResponse>> GetAll();

    Task UpdatePatient(Guid userId, UpdatePatientRequest request);

    Task DeactivatePatient(Guid userId);

    Task DeletePatient(Guid id);
}
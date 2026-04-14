namespace PatientService.Application.Services;

using PatientService.Application.DTOs;
using PatientService.Application.Interfaces;
using PatientService.Domain.Entities;

using Shared.Contracts.Enums;
using Shared.Contracts.Infrastructure.Auth;

public class PatientServiceImplementation : IPatientService
{
    private readonly IPatientRepository _repo;
    private readonly IAuthServiceClient _authClient;

    public PatientServiceImplementation(IPatientRepository repo, IAuthServiceClient authClient)
    {
        _repo = repo;
        _authClient = authClient;
    }

    public async Task<Guid> CreatePatient(Guid userId, CreatePatientRequest request)
    {
        var existing = await _repo.GetByUserIdAsync(userId);

        if (existing != null)
        {
            return existing.Id;
        }

        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FullName = request.FullName,
            Email = request.Email,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(patient);
        await _repo.SaveChangesAsync();

        try
        {
            await _authClient.GrantRoleAsync(userId, UserRole.Patient);
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to grant patient role" + ex);
        }

        return patient.Id;
    }

    public async Task<PatientResponse?> GetByUserId(Guid userId)
    {
        var patient = await _repo.GetByUserIdAsync(userId);

        if (patient == null || !patient.IsActive)
        {
            return null;
        }

        return Map(patient);
    }

    public async Task<PatientResponse?> GetById(Guid id)
    {
        var patient = await _repo.GetByIdAsync(id);

        if (patient == null || !patient.IsActive)
        {
            return null;
        }

        return Map(patient);
    }

    public async Task<List<PatientResponse>> GetAll()
    {
        var patients = await _repo.GetAllAsync();

        return patients
            .Where(p => p.IsActive)
            .Select(Map)
            .ToList();
    }

    public async Task UpdatePatient(Guid userId, UpdatePatientRequest request)
    {
        var patient = await _repo.GetByUserIdAsync(userId);

        if (patient == null)
        {
            throw new Exception("Patient not found");
        }

        if (!patient.IsActive)
        {
            throw new Exception("Patient is deactivated");
        }

        patient.FullName = request.FullName;
        patient.UpdatedAt = DateTime.UtcNow;

        await _repo.SaveChangesAsync();
    }

    public async Task DeactivatePatient(Guid userId)
    {
        var patient = await _repo.GetByUserIdAsync(userId);

        if (patient == null)
        {
            throw new Exception("Patient not found");
        }

        if (!patient.IsActive)
        {
            throw new Exception("Patient already deactivated");
        }

        patient.IsActive = false;
        patient.UpdatedAt = DateTime.UtcNow;

        await _repo.SaveChangesAsync();
    }

    public async Task DeletePatient(Guid id)
    {
        var patient = await _repo.GetByIdAsync(id);

        if (patient == null)
        {
            throw new Exception("Patient not found");
        }

        await _repo.RemoveAsync(patient);
        await _repo.SaveChangesAsync();
    }

    // mapper
    private static PatientResponse Map(Patient p) => new()
    {
        Id = p.Id,
        UserId = p.UserId,
        FullName = p.FullName,
        Email = p.Email
    };
}

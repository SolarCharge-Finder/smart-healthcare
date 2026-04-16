namespace Doctor.Application.Services;

using Doctor.Application.DTOs;
using Doctor.Application.Interfaces;
using Doctor.Domain.Entities;

using Shared.Contracts.Enums;
using Shared.Contracts.Infrastructure.Auth;

public class DoctorService : IDoctorService
{
    private readonly IDoctorRepository _repo;
    private readonly IAuthServiceClient _authClient;
    public DoctorService(IDoctorRepository repo, IAuthServiceClient authClient)
    {
        _repo = repo;
        _authClient = authClient;
    }

    public async Task<Guid> CreateDoctor(CreateDoctorRequest request, Guid userId)
    {
        var existing = await _repo.GetByUserIdAsync(userId);
        if (existing != null)
        {
            throw new InvalidOperationException("Doctor profile already exists");
        }

        var doctor = new Doctor
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FullName = request.FullName,
            Specialization = request.Specialization,
            Hospital = request.Hospital,
            IsApproved = false,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(doctor);
        await _repo.SaveChangesAsync();

        return doctor.Id;
    }

    public async Task ApproveDoctor(Guid id)
    {
        var doctor = await _repo.GetByIdAsync(id);

        if (doctor == null)
        {
            throw new Exception("Doctor not found");
        }

        if (doctor.IsApproved)
        {
            throw new Exception("Doctor already approved");
        }

        doctor.IsApproved = true;

        await _repo.SaveChangesAsync();

        try
        {
            await _authClient.GrantRoleAsync(doctor.UserId, UserRole.Doctor);
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to grant doctor role", ex);
        }
    }

    public async Task<List<DoctorResponse>> GetAll()
    {
        var doctors = await _repo.GetAllAsync();

        return doctors.Select(Map).ToList();
    }

    public async Task<List<DoctorResponse>> GetApproved()
    {
        var doctors = await _repo.GetApprovedAsync();

        return doctors.Select(Map).ToList();
    }

    public async Task<List<DoctorResponse>> SearchDoctors(SearchDoctorsRequest request)
    {
        var doctors = await _repo.SearchAsync(
            request.Name,
            request.Specialization,
            request.Hospital,
            request.Date
        );

        return doctors.Select(Map).ToList();
    }

    public async Task<FilterOptionsResponse> GetFilterOptionsAsync()
    {
        var doctors = await _repo.GetApprovedAsync();

        var response = new FilterOptionsResponse
        {
            DoctorNames = doctors.Select(d => d.FullName).ToList(),
            Specializations = doctors.Select(d => d.Specialization).Distinct().ToList(),
            Hospitals = doctors.Select(d => d.Hospital).Distinct().ToList()
        };

        return response;
    }

    public async Task<List<DoctorResponse>> GetPending()
    {
        var doctors = await _repo.GetPendingAsync();

        return doctors.Select(Map).ToList();
    }

    public async Task<DoctorResponse?> GetById(Guid id)
    {
        var doctor = await _repo.GetByIdAsync(id);

        if (doctor == null)
        {
            return null;
        }

        return Map(doctor);
    }

    public async Task<DoctorResponse?> GetByUserId(Guid userId)
    {
        var doctor = await _repo.GetByUserIdAsync(userId);

        if (doctor == null)
        {
            return null;
        }

        return Map(doctor);
    }

    public async Task DeleteDoctor(Guid id)
    {
        var doctor = await _repo.GetByIdAsync(id);

        if (doctor == null)
        {
            throw new Exception("Doctor not found");
        }

        await _repo.RemoveAsync(doctor);
        await _repo.SaveChangesAsync();
    }

    public async Task<decimal> GetConsultationFee(Guid doctorId)
    {
        var doctor = await _repo.GetByIdAsync(doctorId);

        if (doctor == null)
        {
            throw new Exception("Doctor not found");
        }

        if (!doctor.IsApproved)
        {
            throw new Exception("Doctor is not approved");
        }

        return doctor.ConsultationFee;
    }

    public async Task UpdateConsultationFee(Guid doctorId, decimal fee, Guid userId)
    {
        var doctor = await _repo.GetByIdAsync(doctorId);

        if (doctor == null)
        {
            throw new Exception("Doctor not found");
        }

        // Only the doctor who owns this profile can update
        if (doctor.UserId != userId)
        {
            throw new Exception("Unauthorized to update consultation fee");
        }

        // Validation
        if (fee < 0)
        {
            throw new Exception("Consultation fee cannot be negative");
        }

        doctor.ConsultationFee = fee;

        await _repo.SaveChangesAsync();
    }

    private static DoctorResponse Map(Doctor d) => new()
    {
        Id = d.Id,
        UserId = d.UserId,
        FullName = d.FullName,
        Specialization = d.Specialization,
        Hospital = d.Hospital,
        IsApproved = d.IsApproved
    };
}

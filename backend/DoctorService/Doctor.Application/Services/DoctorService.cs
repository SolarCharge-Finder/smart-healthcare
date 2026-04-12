namespace Doctor.Application.Services;

using Doctor.Application.DTOs;
using Doctor.Application.Interfaces;
using Doctor.Domain.Entities;

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

        await _authClient.GrantRoleAsync(doctor.UserId, "Doctor"); 
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

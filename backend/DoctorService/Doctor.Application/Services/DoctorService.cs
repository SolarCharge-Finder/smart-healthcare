namespace Doctor.Application.Services;

using Doctor.Application.Interfaces;
using Doctor.Application.DTOs;
using Doctor.Domain.Entities;

public class DoctorService : IDoctorService
{
    private readonly IDoctorRepository _repo;

    public DoctorService(IDoctorRepository repo)
    {
        _repo = repo;
    }

    public async Task<Guid> CreateDoctor(CreateDoctorRequest request, Guid userId)
    {
        // prevent duplicate doctor profiles
        var existing = await _repo.GetByUserIdAsync(userId);
        if (existing != null)
            throw new InvalidOperationException("Doctor profile already exists");

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
            throw new Exception("Doctor not found");

        if (doctor.IsApproved)
            throw new Exception("Doctor already approved");

        doctor.IsApproved = true;

        await _repo.SaveChangesAsync();
    }

    public async Task<List<Doctor>> GetAll()
    {
        return await _repo.GetAllAsync();
    }

    public async Task<List<Doctor>> GetPending()
    {
        return await _repo.GetPendingAsync();
    }

    public async Task<Doctor?> GetByUserId(Guid userId)
    {
        return await _repo.GetByUserIdAsync(userId);
    }
}
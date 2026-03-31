namespace Doctor.Application.Services;

using Doctor.Application.Interfaces;
using Doctor.Domain.Entities;

public class DoctorService : IDoctorService
{
    private readonly IDoctorRepository _repo;

    public DoctorService(IDoctorRepository repo)
    {
        _repo = repo;
    }

    public async Task CreateDoctor(CreateDoctorRequest request)
    {
        var doctor = new Doctor
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            FullName = request.FullName,
            Specialization = request.Specialization,
            Hospital = request.Hospital,
            IsApproved = false,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(doctor);
        await _repo.SaveChangesAsync();
    }

    public async Task ApproveDoctor(Guid doctorId)
    {
        var doctor = await _repo.GetByIdAsync(doctorId);

        if (doctor == null)
            throw new Exception("Doctor not found");

        doctor.IsApproved = true;

        await _repo.SaveChangesAsync();
    }
}
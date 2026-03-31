namespace Doctor.Application.Interfaces;

using Doctor.Application.DTOs;
using Doctor.Domain.Entities;

public interface IDoctorService
{
    Task<Guid> CreateDoctor(CreateDoctorRequest request);

    Task ApproveDoctor(Guid id);

    Task<List<Doctor>> GetAll();

    Task<List<Doctor>> GetPending();

    Task<Doctor?> GetByUserId(Guid userId);
}
namespace Doctor.Application.Interfaces;

using Doctor.Application.DTOs;

public interface IDoctorService
{
    Task<Guid> CreateDoctor(CreateDoctorRequest request, Guid userId);

    Task ApproveDoctor(Guid id);

    Task<List<DoctorResponse>> GetAll();

    Task<List<DoctorResponse>> GetApproved();

    Task<List<DoctorResponse>> GetPending();

    Task<DoctorResponse?> GetById(Guid id);

    Task DeleteDoctor(Guid id);
}

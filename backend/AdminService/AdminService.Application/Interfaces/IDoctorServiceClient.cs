namespace AdminService.Application.Interfaces;

using AdminService.Application.DTOs;

public interface IDoctorServiceClient
{
    Task<List<DoctorResponse>> GetPendingDoctors();
    Task ApproveDoctor(Guid id);
}

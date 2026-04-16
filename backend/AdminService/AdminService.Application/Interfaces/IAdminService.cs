namespace AdminService.Application.Interfaces;

using AdminService.Application.DTOs;

public interface IAdminService
{
    Task CreateAdmin(Guid userId, CreateAdminRequest request);
    Task<List<AdminResponse>> GetAll();
    Task<AdminResponse?> GetById(Guid id);
    Task<List<AdminResponse>> GetPending();
    Task ApproveAdmin(Guid id);
    Task RejectAdmin(Guid id);
    Task<List<DoctorResponse>> GetPendingDoctors();
    Task ApproveDoctor(Guid id);
}

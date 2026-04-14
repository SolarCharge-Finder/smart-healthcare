namespace AdminService.Application.Services;

using AdminService.Application.DTOs;
using AdminService.Application.Interfaces;
using AdminService.Domain.Entities;

public class AdminServiceImplementation : IAdminService
{
    private readonly IAdminRepository _repo;
    private readonly IDoctorServiceClient _doctorClient;


    public AdminServiceImplementation(IAdminRepository repo, IDoctorServiceClient doctorClient)
    {
        _repo = repo;
        _doctorClient = doctorClient;
    }

    public async Task CreateAdmin(Guid userId, CreateAdminRequest request)
    {
        var existing = await _repo.GetByUserIdAsync(userId);

        if (existing != null)
        {
            throw new Exception("Admin already exists");
        }

        var admin = new Admin
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FullName = request.FullName,
            IsApproved = false
        };

        await _repo.AddAsync(admin);
        await _repo.SaveChangesAsync();
    }

    public async Task<List<AdminResponse>> GetAll()
    {
        var admins = await _repo.GetAllAsync();

        return admins.Select(a => new AdminResponse
        {
            Id = a.Id,
            FullName = a.FullName,
            IsApproved = a.IsApproved
        }).ToList();
    }

    public async Task<List<AdminResponse>> GetPending()
    {
        var admins = await _repo.GetPendingAsync();

        return admins.Select(a => new AdminResponse
        {
            Id = a.Id,
            FullName = a.FullName,
            IsApproved = a.IsApproved
        }).ToList();
    }

    public async Task ApproveAdmin(Guid id)
    {
        var admin = await _repo.GetByIdAsync(id);

        if (admin == null)
        {
            throw new Exception("Admin not found");
        }

        if (admin.IsApproved)
        {
            throw new Exception("Admin already approved");
        }

        admin.IsApproved = true;

        await _repo.SaveChangesAsync();
    }

    public async Task RejectAdmin(Guid id)
    {
        var admin = await _repo.GetByIdAsync(id);

        if (admin == null)
        {
            throw new Exception("Admin not found");
        }

        await _repo.RemoveAsync(admin);

        await _repo.SaveChangesAsync();
    }

    public async Task<List<DoctorResponse>> GetPendingDoctors()
    {
        return await _doctorClient.GetPendingDoctors();
    }

    public async Task ApproveDoctor(Guid id)
    {
        await _doctorClient.ApproveDoctor(id);
    }
}

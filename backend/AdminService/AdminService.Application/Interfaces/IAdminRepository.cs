namespace AdminService.Application.Interfaces;

using AdminService.Domain.Entities;

public interface IAdminRepository
{
    Task AddAsync(Admin admin);

    Task<Admin?> GetByIdAsync(Guid id);

    Task<Admin?> GetByUserIdAsync(Guid userId);

    Task<List<Admin>> GetAllAsync();

    Task<List<Admin>> GetPendingAsync();

    Task SaveChangesAsync();

    Task RemoveAsync(Admin admin);
}

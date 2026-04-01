namespace AdminService.Infrastructure.Repositories;

using AdminService.Application.Interfaces;
using AdminService.Domain.Entities;
using AdminService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class AdminRepository : IAdminRepository
{
    private readonly AdminDbContext _context;

    public AdminRepository(AdminDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Admin admin)
    {
        await _context.Admins.AddAsync(admin);
    }

    public async Task<Admin?> GetByIdAsync(Guid id)
    {
        return await _context.Admins.FindAsync(id);
    }

    public async Task<Admin?> GetByUserIdAsync(Guid userId)
    {
        return await _context.Admins
            .FirstOrDefaultAsync(x => x.UserId == userId);
    }

    public async Task<List<Admin>> GetAllAsync()
    {
        return await _context.Admins.ToListAsync();
    }

    public async Task<List<Admin>> GetPendingAsync()
    {
        return await _context.Admins
            .Where(x => !x.IsApproved)
            .ToListAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task RemoveAsync(Admin admin)
    {
        _context.Admins.Remove(admin);
        await _context.SaveChangesAsync();
    }
}
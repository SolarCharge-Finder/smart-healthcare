namespace AdminService.Infrastructure.Repositories;

using AdminService.Application.Interfaces;
using AdminService.Domain.Entities;
using AdminService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class BaseRepository<T> where T : BaseEntity
{
    protected readonly AdminServiceDbContext _context;

    public BaseRepository(AdminServiceDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(T entity)
    {
        await _context.Set<T>().AddAsync(entity);
    }

    public async Task<T?> GetByIdAsync(Guid id)
    {
        return await _context.Set<T>().FindAsync(id);
    }
}
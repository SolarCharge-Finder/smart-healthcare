namespace SmartService.Infrastructure.Repositories;

using SmartService.Application.Interfaces;
using SmartService.Domain.Entities;
using SmartService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class BaseRepository<T> where T : BaseEntity
{
    protected readonly SmartServiceDbContext _context;

    public BaseRepository(SmartServiceDbContext context)
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
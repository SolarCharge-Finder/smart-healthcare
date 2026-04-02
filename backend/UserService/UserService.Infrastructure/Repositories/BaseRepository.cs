namespace UserService.Infrastructure.Repositories;

using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using UserService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class BaseRepository<T> where T : BaseEntity
{
    protected readonly UserServiceDbContext _context;

    public BaseRepository(UserServiceDbContext context)
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
namespace Auth.Infrastructure.Repositories;

using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using Auth.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

public class UserRepository : IUserRepository
{
    private readonly AuthDbContext _context;
    private IDbContextTransaction? _transaction;

    public UserRepository(AuthDbContext context)
    {
        _context = context;
    }

    // transaction management

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
            await _transaction.CommitAsync();
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
            await _transaction.RollbackAsync();
    }

    // user

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await _context.Users.AnyAsync(u => u.Email == email);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
    }
    
    // pending user
    public async Task<PendingUser?> GetPendingByEmailAsync(string email)
    {
        return await _context.PendingUsers
            .FirstOrDefaultAsync(p => p.Email == email);
    }

    public async Task<PendingUser?> GetPendingByTokenAsync(string token)
    {
        return await _context.PendingUsers
            .FirstOrDefaultAsync(p => p.VerificationToken == token);
    }

    public async Task<PendingUser?> GetRecentPendingByEmailAsync(string email)
    {
        return await _context.PendingUsers
            .Where(p => p.Email == email)
            .OrderByDescending(p => p.ExpiresAt)
            .FirstOrDefaultAsync();
    }

    public async Task AddPendingAsync(PendingUser pending)
    {
        await _context.PendingUsers.AddAsync(pending);
    }

    public void RemovePending(PendingUser pending)
    {
        _context.PendingUsers.Remove(pending);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
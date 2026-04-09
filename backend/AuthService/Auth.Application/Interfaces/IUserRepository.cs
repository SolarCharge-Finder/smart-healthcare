namespace Auth.Application.Interfaces;

using Auth.Domain.Entities;

public interface IUserRepository
{
    //related to wrapping register in trasactions 
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
    Task<bool> ExistsByEmailAsync(string email);
    Task<User?> GetByIdAsync(Guid userId);
    Task<PendingUser?> GetPendingByEmailAsync(string email);
    Task<PendingUser?> GetPendingByTokenAsync(string token);
    Task<User?> GetByPasswordResetTokenAsync(string token);
    //rate limiting
    Task<PendingUser?> GetRecentPendingByEmailAsync(string email);
    Task AddAsync(User user);
    Task AddPendingAsync(PendingUser pending);
    void RemovePending(PendingUser pending);
    Task<User?> GetByEmailAsync(string email);
    Task SaveChangesAsync();
}
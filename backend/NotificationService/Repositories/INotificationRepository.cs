using NotificationService.Models;

namespace NotificationService.Repositories;

public interface INotificationRepository
{
    Task<IReadOnlyList<NotificationEntity>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<NotificationEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(NotificationEntity notification, CancellationToken cancellationToken = default);
    Task<int> MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

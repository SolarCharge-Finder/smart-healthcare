using NotificationService.DTOs;
using NotificationService.Repositories;

namespace NotificationService.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _repository;

    public NotificationService(INotificationRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<NotificationDto>> GetUserNotificationsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var notifications = await _repository.GetByUserAsync(userId, cancellationToken);

        return notifications
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                UserId = n.UserId,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                RelatedEntityId = n.RelatedEntityId
            })
            .ToList();
    }

    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _repository.GetUnreadCountAsync(userId, cancellationToken);
    }

    public async Task<bool> MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _repository.GetByIdAsync(notificationId, cancellationToken);

        if (notification is null || notification.UserId != userId)
        {
            return false;
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _repository.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    public async Task<int> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var updated = await _repository.MarkAllReadAsync(userId, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return updated;
    }
}

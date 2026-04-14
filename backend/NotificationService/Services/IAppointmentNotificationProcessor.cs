using NotificationService.Events;

namespace NotificationService.Services;

public interface IAppointmentNotificationProcessor
{
    Task ProcessAsync(string eventName, AppointmentEventPayload payload, CancellationToken cancellationToken = default);
    Task<int> GenerateRemindersAsync(CancellationToken cancellationToken = default);
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using NotificationService.Data;
using NotificationService.Events;
using NotificationService.Models;

namespace NotificationService.Services;

public class AppointmentNotificationProcessor : IAppointmentNotificationProcessor
{
    private readonly NotificationDbContext _dbContext;
    private readonly ILogger<AppointmentNotificationProcessor> _logger;

    public AppointmentNotificationProcessor(
        NotificationDbContext dbContext,
        ILogger<AppointmentNotificationProcessor> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task ProcessAsync(
        string eventName,
        AppointmentEventPayload payload,
        CancellationToken cancellationToken = default)
    {
        if (payload.Id == Guid.Empty)
        {
            _logger.LogWarning("Skipping event {EventName}: appointment id is missing.", eventName);
            return;
        }

        var userId = payload.UserId.GetValueOrDefault();
        if (userId == Guid.Empty)
        {
            _logger.LogInformation(
                "Skipping notification for appointment {AppointmentId}: user id is missing (likely guest flow).",
                payload.Id);
            return;
        }

        var slotTime = payload.SlotTime?.ToUniversalTime() ?? DateTime.UtcNow;
        var status = ResolveStatusForProjection(eventName, payload.Status);
        var eventKey = BuildEventKey(eventName, payload.Id, userId, status, slotTime);

        var alreadyProcessed = await _dbContext.ProcessedEvents
            .AsNoTracking()
            .AnyAsync(x => x.EventKey == eventKey, cancellationToken);

        if (alreadyProcessed)
        {
            return;
        }

        var (type, title, message) = BuildNotification(eventName);

        var notification = new NotificationEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            RelatedEntityId = payload.Id
        };

        var projection = await _dbContext.AppointmentProjections
            .FirstOrDefaultAsync(x => x.AppointmentId == payload.Id, cancellationToken);

        if (projection is null)
        {
            projection = new AppointmentProjectionEntity
            {
                Id = Guid.NewGuid(),
                AppointmentId = payload.Id,
                UserId = userId,
                Status = status,
                SlotTimeUtc = slotTime,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.AppointmentProjections.Add(projection);
        }
        else
        {
            projection.Status = status;
            projection.SlotTimeUtc = slotTime;
            projection.UpdatedAt = DateTime.UtcNow;

            if (status is "CANCELLED" or "DECLINED")
            {
                projection.ReminderSentAt = null;
            }
        }

        await _dbContext.Notifications.AddAsync(notification, cancellationToken);

        await _dbContext.ProcessedEvents.AddAsync(
            new ProcessedEventEntity
            {
                Id = Guid.NewGuid(),
                EventKey = eventKey,
                EventName = eventName,
                ProcessedAt = DateTime.UtcNow
            },
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        NotificationMetrics.IncEventsConsumed();
        NotificationMetrics.IncNotificationsCreated();
    }

    public async Task<int> GenerateRemindersAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var reminderCutoff = now.AddHours(24);

        var candidates = await _dbContext.AppointmentProjections
            .Where(x =>
                x.ReminderSentAt == null &&
                x.SlotTimeUtc > now &&
                x.SlotTimeUtc <= reminderCutoff &&
                x.Status != "CANCELLED" &&
                x.Status != "DECLINED")
            .OrderBy(x => x.SlotTimeUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        foreach (var item in candidates)
        {
            await _dbContext.Notifications.AddAsync(
                new NotificationEntity
                {
                    Id = Guid.NewGuid(),
                    UserId = item.UserId,
                    Title = "Appointment Reminder",
                    Message = "Reminder: you have an appointment tomorrow.",
                    Type = NotificationType.Reminder,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    RelatedEntityId = item.AppointmentId
                },
                cancellationToken);

            item.ReminderSentAt = DateTime.UtcNow;
            item.UpdatedAt = DateTime.UtcNow;
        }

        if (candidates.Count > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            NotificationMetrics.IncNotificationsCreated(candidates.Count);
            NotificationMetrics.IncRemindersCreated(candidates.Count);
        }

        return candidates.Count;
    }

    private static (string Type, string Title, string Message) BuildNotification(string eventName)
    {
        return eventName switch
        {
            AppointmentEventNames.Created =>
                (NotificationType.AppointmentPending, "Appointment Pending", "Your appointment request is pending confirmation."),
            AppointmentEventNames.Confirmed =>
                (NotificationType.Confirmed, "Appointment Confirmed", "Your appointment has been confirmed."),
            AppointmentEventNames.PaymentConfirmed =>
                (NotificationType.Confirmed, "Payment Confirmed", "Your appointment payment was successful."),
            AppointmentEventNames.Cancelled =>
                (NotificationType.Cancelled, "Appointment Cancelled", "Your appointment has been cancelled."),
            AppointmentEventNames.Declined =>
                (NotificationType.Declined, "Appointment Declined", "Your appointment request was declined."),
            _ =>
                (NotificationType.AppointmentPending, "Appointment Update", "Your appointment has been updated.")
        };
    }

    private static string ResolveStatusForProjection(string eventName, string? status)
    {
        if (!string.IsNullOrWhiteSpace(status))
        {
            return status.ToUpperInvariant();
        }

        return eventName switch
        {
            AppointmentEventNames.Created => "PENDING_PAYMENT",
            AppointmentEventNames.Confirmed => "CONFIRMED",
            AppointmentEventNames.PaymentConfirmed => "PAID",
            AppointmentEventNames.Cancelled => "CANCELLED",
            AppointmentEventNames.Declined => "DECLINED",
            _ => "PENDING_PAYMENT"
        };
    }

    private static string BuildEventKey(
        string eventName,
        Guid appointmentId,
        Guid userId,
        string status,
        DateTime slotTimeUtc)
    {
        return $"{eventName}:{appointmentId}:{userId}:{status}:{slotTimeUtc:O}";
    }
}

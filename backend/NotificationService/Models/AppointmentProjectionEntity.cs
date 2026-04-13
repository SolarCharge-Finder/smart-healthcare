using System.ComponentModel.DataAnnotations;

namespace NotificationService.Models;

public class AppointmentProjectionEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid AppointmentId { get; set; }

    public Guid UserId { get; set; }

    [MaxLength(40)]
    public string Status { get; set; } = string.Empty;

    public DateTime SlotTimeUtc { get; set; }

    public DateTime? ReminderSentAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace NotificationService.Models;

public class ProcessedEventEntity
{
    [Key]
    public Guid Id { get; set; }

    [MaxLength(220)]
    public string EventKey { get; set; } = string.Empty;

    [MaxLength(80)]
    public string EventName { get; set; } = string.Empty;

    public DateTime ProcessedAt { get; set; }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NotificationService.Models;

public class NotificationEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    [MaxLength(140)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;

    [MaxLength(40)]
    public string Type { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? RelatedEntityId { get; set; }

    [Column(TypeName = "timestamp with time zone")]
    public DateTime? ReadAt { get; set; }
}

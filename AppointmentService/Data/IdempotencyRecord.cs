namespace AppointmentService.Data;

public class IdempotencyRecord
{
    public Guid Id { get; set; }

    public string Key { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public Guid AppointmentId { get; set; }
}

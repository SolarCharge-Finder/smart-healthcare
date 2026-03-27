namespace AppointmentService.Data;

public class OutboxMessage
{
    public Guid Id { get; set; }

    public string EventName { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public string CorrelationId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }
}

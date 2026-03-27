namespace AppointmentService.Logging;

public interface ICorrelationIdAccessor
{
    string? CorrelationId { get; set; }
}

public class CorrelationIdAccessor : ICorrelationIdAccessor
{
    private static readonly AsyncLocal<string?> _current = new();

    public string? CorrelationId
    {
        get => _current.Value;
        set => _current.Value = value;
    }
}

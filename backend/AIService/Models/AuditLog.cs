namespace AIService.Models;

public class AuditLog
{
    public Guid Id { get; set; }
    public Guid? PatientId { get; set; }
    public string RequestIp { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string RequestData { get; set; } = string.Empty;
    public string ResponseData { get; set; } = string.Empty;
    public int ResponseTimeMs { get; set; }
    public bool Success { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public int ApiTokensUsed { get; set; }
    public decimal CostUsd { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

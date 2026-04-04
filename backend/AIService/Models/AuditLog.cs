namespace AIService.Models;

public class AuditLog
{
    public Guid Id { get; set; }
    public Guid? PatientId { get; set; }
    public string Action { get; set; } = string.Empty;  // e.g., "SymptomCheck"
    public string InputData { get; set; } = string.Empty;  // User input (symptoms)
    public string ResultData { get; set; } = string.Empty;  // AI result (JSON)
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public int ResponseTimeMs { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    // Legacy properties (kept for backward compatibility)
    public string RequestIp { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string RequestData { get; set; } = string.Empty;
    public string ResponseData { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public int ApiTokensUsed { get; set; }
    public decimal CostUsd { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

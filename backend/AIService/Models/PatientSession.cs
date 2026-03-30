namespace AIService.Models;

public class PatientSession
{
    public Guid Id { get; set; }
    
    public Guid PatientId { get; set; }
    
    public string SessionToken { get; set; } = string.Empty;
    
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    
    public DateTime ExpiresAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public int AnalysisCount { get; set; } = 0;
    
    public string? Notes { get; set; }
    
    public string CreatedBy { get; set; } = string.Empty;
    
    public string CorrelationId { get; set; } = string.Empty;
}

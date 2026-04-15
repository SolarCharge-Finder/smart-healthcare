using AIService.Data;
using AIService.DTOs;
using AIService.Models;
using Serilog.Context;

namespace AIService.Services;

public interface IAuditLoggingService
{
    Task LogSymptomCheckAsync(
        Guid? patientId,
        string symptoms,
        SymptomAnalysisResponse response,
        string ipAddress,
        int responseTimeMs,
        string correlationId);

    Task<IEnumerable<AuditLog>> GetUserAuditLogsAsync(Guid patientId, int days = 30);
    Task<IEnumerable<AuditLog>> GetAllAuditLogsAsync(int days = 30);
}

/// <summary>
/// Comprehensive audit logging service
/// CRITICAL: Every AI analysis is logged for compliance, debugging, and user transparency
/// 
/// Stores:
/// - User ID / Patient ID
/// - Input symptoms
/// - AI response and confidence  
/// - IP address
/// - Response time
/// - API cost
/// - Timestamp
/// - Correlation ID for tracing
/// </summary>
public class AuditLoggingService : IAuditLoggingService
{
    private readonly AiDbContext _dbContext;
    private readonly ILogger<AuditLoggingService> _logger;

    public AuditLoggingService(AiDbContext dbContext, ILogger<AuditLoggingService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Log a symptom check analysis to the database
    /// </summary>
    public async Task LogSymptomCheckAsync(
        Guid? patientId,
        string symptoms,
        SymptomAnalysisResponse response,
        string ipAddress,
        int responseTimeMs,
        string correlationId)
    {
        using (LogContext.PushProperty("AuditLog", "SymptomCheck"))
        {
            try
            {
                var normalizedUrgency = response.Urgency;

                var auditLog = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    PatientId = patientId,
                    Action = "SymptomCheck",
                    InputData = symptoms,
                    ResultData = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        response.Success,
                        Urgency = normalizedUrgency,
                        response.ConfidenceScore,
                        response.RecommendedSpecialty,
                        response.PossibleConditions,
                        response.TokensUsed,
                        response.CostUsd
                    }),
                    IpAddress = ipAddress,
                    ResponseTimeMs = responseTimeMs,
                    CorrelationId = correlationId,
                    Timestamp = DateTime.UtcNow,
                    UserAgent = ExtractUserAgent()
                };

                _dbContext.AuditLogs.Add(auditLog);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation(
                    "Audit log created: PatientId={PatientId}, " +
                    "Urgency={Urgency}, " +
                    "Confidence={Confidence}, " +
                    "Cost=${Cost}, " +
                    "ResponseTime={ResponseTime}ms, " +
                    "CorrelationId={CorrelationId}",
                    patientId, normalizedUrgency, response.ConfidenceScore,
                    response.CostUsd, responseTimeMs, correlationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error logging audit entry for patient {PatientId}. CorrelationId: {CorrelationId}",
                    patientId, correlationId);
                // Don't throw - audit logging should not break the main flow
            }
        }
    }

    /// <summary>
    /// Get audit logs for a specific patient (for user transparency)
    /// </summary>
    public async Task<IEnumerable<AuditLog>> GetUserAuditLogsAsync(Guid patientId, int days = 30)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);

            var logs = _dbContext.AuditLogs
                .Where(log => log.PatientId == patientId && log.Timestamp >= cutoffDate)
                .OrderByDescending(log => log.Timestamp)
                .ToList();

            _logger.LogInformation(
                "Retrieved {Count} audit logs for patient {PatientId} from last {Days} days",
                logs.Count, patientId, days);

            return logs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs for patient {PatientId}", patientId);
            return new List<AuditLog>();
        }
    }

    /// <summary>
    /// Get all audit logs (admin access)
    /// </summary>
    public async Task<IEnumerable<AuditLog>> GetAllAuditLogsAsync(int days = 30)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);

            var logs = _dbContext.AuditLogs
                .Where(log => log.Timestamp >= cutoffDate)
                .OrderByDescending(log => log.Timestamp)
                .ToList();

            _logger.LogInformation(
                "Retrieved {Count} total audit logs from last {Days} days",
                logs.Count, days);

            return logs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all audit logs");
            return new List<AuditLog>();
        }
    }

    /// <summary>
    /// Extract user agent from HTTP context (for device tracking)
    /// </summary>
    private string ExtractUserAgent()
    {
        // This would normally come from HttpContext
        // For now, return placeholder
        return "Unknown";
    }
}

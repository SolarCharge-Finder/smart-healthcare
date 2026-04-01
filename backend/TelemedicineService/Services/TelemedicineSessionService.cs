using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TelemedicineService.Data;
using TelemedicineService.Models;

namespace TelemedicineService.Services;

/// <summary>
/// Implementation of telemedicine session management.
/// Coordinates with AppointmentService for validation and generates Agora tokens.
/// </summary>
public class TelemedicineSessionService : ITelemedicineService
{
    private readonly TelemedicineDbContext _dbContext;
    private readonly IAgoraTokenService _agoraTokenService;
    private readonly HttpClient _appointmentServiceClient;
    private readonly ILogger<TelemedicineSessionService> _logger;
    private readonly AgoraOptions _agoraOptions;

    public TelemedicineSessionService(
        TelemedicineDbContext dbContext,
        IAgoraTokenService agoraTokenService,
        HttpClient appointmentServiceClient,
        ILogger<TelemedicineSessionService> logger,
        IOptions<AgoraOptions> agoraOptions)
    {
        _dbContext = dbContext;
        _agoraTokenService = agoraTokenService;
        _appointmentServiceClient = appointmentServiceClient;
        _logger = logger;
        _agoraOptions = agoraOptions.Value;
    }

    public async Task<TelemedicineSessionResponse> CreateSessionAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating telemedicine session for appointment: {AppointmentId}", appointmentId);

        // 1. Check if session already exists and is still valid
        var existingSession = await GetActiveSessionAsync(appointmentId, cancellationToken);
        if (existingSession != null)
        {
            _logger.LogInformation(
                "Active session already exists for appointment {AppointmentId}, reusing tokens",
                appointmentId);

            return ConvertSessionToResponse(existingSession);
        }

        // 2. Fetch appointment details from AppointmentService
        var appointment = await FetchAppointmentAsync(appointmentId, cancellationToken);

        if (appointment == null)
        {
            _logger.LogWarning("Appointment not found: {AppointmentId}", appointmentId);
            throw new KeyNotFoundException($"Appointment {appointmentId} not found.");
        }

        // 3. Validate appointment status
        ValidateAppointmentStatus(appointment);

        // 4. Generate channel name
        var channelName = $"appointment-{appointmentId}";

        // 5. Generate Agora tokens (server-side only)
        var patientToken = _agoraTokenService.GenerateToken(channelName, _agoraOptions.TokenExpirySeconds);
        var doctorToken = _agoraTokenService.GenerateToken(channelName, _agoraOptions.TokenExpirySeconds);

        // 6. Create and persist session record
        var expiresAt = DateTime.UtcNow.AddSeconds(_agoraOptions.TokenExpirySeconds);
        var session = new TelemedicineSession
        {
            AppointmentId = appointmentId,
            ChannelName = channelName,
            TokenExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.TelemedicineSessions.Add(session);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Successfully created telemedicine session for appointment {AppointmentId} on channel {ChannelName}",
            appointmentId, channelName);

        // 7. Return response with tokens
        return new TelemedicineSessionResponse
        {
            AppointmentId = appointmentId,
            ChannelName = channelName,
            PatientToken = patientToken,
            DoctorToken = doctorToken,
            ExpiresAt = expiresAt
        };
    }

    public async Task<TelemedicineSession?> GetActiveSessionAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        var session = await _dbContext.TelemedicineSessions
            .FirstOrDefaultAsync(s => s.AppointmentId == appointmentId, cancellationToken);

        // Return null if session is expired
        if (session != null && session.TokenExpiresAt < DateTime.UtcNow)
        {
            _logger.LogInformation("Session for appointment {AppointmentId} has expired", appointmentId);
            return null;
        }

        return session;
    }

    private async Task<AppointmentDto?> FetchAppointmentAsync(
        Guid appointmentId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Fetching appointment details from AppointmentService for appointment: {AppointmentId}",
                appointmentId);

            var appointment = await _appointmentServiceClient.GetFromJsonAsync<AppointmentDto>(
                $"appointments/{appointmentId}",
                cancellationToken);

            return appointment;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning(
                ex,
                "Appointment {AppointmentId} not found in AppointmentService",
                appointmentId);
            return null;
        }
        catch (HttpRequestException ex) when (ex.StatusCode.HasValue)
        {
            _logger.LogError(
                ex,
                "Error fetching appointment {AppointmentId}: {StatusCode}",
                appointmentId, ex.StatusCode);
            throw new InvalidOperationException(
                $"Failed to fetch appointment details. Status: {ex.StatusCode}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error fetching appointment {AppointmentId}",
                appointmentId);
            throw;
        }
    }

    private void ValidateAppointmentStatus(AppointmentDto appointment)
    {
        // Per architecture: Only PAID appointments can generate telemedicine tokens
        // Token generation happens AFTER payment is successfully completed
        var validStatuses = new[] { "Paid", "PAID" };

        if (!validStatuses.Contains(appointment.Status, StringComparer.OrdinalIgnoreCase))
        {
            var message = $"Appointment {appointment.Id} must be 'Paid' to start telemedicine session. " +
                         $"Current status: '{appointment.Status}'. " +
                         $"Payment must be completed first. Please complete payment before joining video.";

            _logger.LogWarning(message);
            throw new InvalidOperationException(message);
        }

        _logger.LogInformation(
            "Appointment {AppointmentId} validated for telemedicine. Status: {Status}. Token generation authorized.",
            appointment.Id, appointment.Status);
    }

    private TelemedicineSessionResponse ConvertSessionToResponse(TelemedicineSession session)
    {
        // When reusing an existing session, we cannot regenerate the tokens
        // (they were already generated and distributed to the frontend)
        // So we return a response indicating the session exists but we return empty tokens
        // The frontend should use the tokens it already has

        // In practice, you might want to:
        // 1. Store the tokens in the database (encrypted)
        // 2. Return them here
        // 3. Or, tell the frontend the session exists and it should use cached tokens
        // 4. Or, generate fresh tokens and update the database

        // For this MVP, we'll generate fresh tokens (valid until the same expiry)
        var channelName = session.ChannelName;
        var secondsUntilExpiry = (uint)Math.Max(0, (session.TokenExpiresAt - DateTime.UtcNow).TotalSeconds);

        return new TelemedicineSessionResponse
        {
            AppointmentId = session.AppointmentId,
            ChannelName = channelName,
            PatientToken = _agoraTokenService.GenerateToken(channelName, secondsUntilExpiry),
            DoctorToken = _agoraTokenService.GenerateToken(channelName, secondsUntilExpiry),
            ExpiresAt = session.TokenExpiresAt
        };
    }
}

/// <summary>
/// DTO for appointment details from AppointmentService.
/// Only includes fields needed for validation.
/// </summary>
public class AppointmentDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
}

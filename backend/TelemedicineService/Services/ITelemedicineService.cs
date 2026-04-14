using TelemedicineService.Models;

namespace TelemedicineService.Services;

/// <summary>
/// Business logic service for telemedicine sessions.
/// Orchestrates appointment validation, token generation, and session persistence.
/// </summary>
public interface ITelemedicineService
{
    /// <summary>
    /// Creates a new telemedicine session for a given appointment.
    /// 
    /// Flow:
    /// 1. Fetches appointment details from AppointmentService
    /// 2. Validates appointment status (must be "Paid" or "Confirmed")
    /// 3. Generates Agora tokens server-side
    /// 4. Stores session in database
    /// 5. Returns tokens to be sent to frontend
    /// </summary>
    /// <param name="appointmentId">The appointment to create session for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Session response with tokens; throws if validation fails</returns>
    /// <exception cref="KeyNotFoundException">Appointment not found</exception>
    /// <exception cref="InvalidOperationException">Appointment not in valid state (not Paid/Confirmed)</exception>
    Task<TelemedicineSessionResponse> CreateSessionAsync(
        Guid appointmentId,
        Guid requesterUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an existing telemedicine session for an appointment.
    /// Used to check if session already exists (avoid duplicate token generation).
    /// </summary>
    /// <param name="appointmentId">The appointment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Session if exists; null if not found or expired</returns>
    Task<TelemedicineSession?> GetActiveSessionAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default);
}

namespace TelemedicineService.Models;

/// <summary>
/// Request to create a telemedicine session for an appointment.
/// </summary>
public class CreateSessionRequest
{
    /// <summary>
    /// The appointment ID for which to create a video session.
    /// </summary>
    public Guid AppointmentId { get; set; }
}

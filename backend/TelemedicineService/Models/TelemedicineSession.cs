namespace TelemedicineService.Models;

/// <summary>
/// Represents a telemedicine video session for an appointment.
/// Stores session metadata and token expiry information.
/// </summary>
public class TelemedicineSession
{
    /// <summary>
    /// Unique identifier for the telemedicine session.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The appointment this session is associated with.
    /// </summary>
    public Guid AppointmentId { get; set; }

    /// <summary>
    /// Agora channel name for the video session.
    /// Format: "appointment-{appointmentId}"
    /// </summary>
    public string ChannelName { get; set; } = string.Empty;

    /// <summary>
    /// When the Agora tokens expire.
    /// </summary>
    public DateTime TokenExpiresAt { get; set; }

    /// <summary>
    /// When the session record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

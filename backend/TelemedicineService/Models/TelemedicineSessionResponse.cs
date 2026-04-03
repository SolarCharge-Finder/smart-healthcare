namespace TelemedicineService.Models;

/// <summary>
/// Response containing Agora tokens and session information for frontend connection.
/// Tokens are generated server-side only and never exposed to the frontend during generation.
/// </summary>
public class TelemedicineSessionResponse
{
    /// <summary>
    /// The appointment ID this session is for.
    /// </summary>
    public Guid AppointmentId { get; set; }

    /// <summary>
    /// Agora Application ID used by the frontend to initialize the RTC client.
    /// </summary>
    public string AgoraAppId { get; set; } = string.Empty;

    /// <summary>
    /// Agora channel name. Frontend uses this to join the channel.
    /// </summary>
    public string ChannelName { get; set; } = string.Empty;

    /// <summary>
    /// Agora RTC token for patient access to the channel.
    /// Generated server-side with publisher role.
    /// </summary>
    public string PatientToken { get; set; } = string.Empty;

    /// <summary>
    /// Agora RTC token for doctor access to the channel.
    /// Generated server-side with publisher role.
    /// </summary>
    public string DoctorToken { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when the tokens expire.
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}

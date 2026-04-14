using System.Text.Json.Serialization;

namespace NotificationService.Events;

public class AppointmentEventPayload
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("userId")]
    public Guid? UserId { get; set; }

    [JsonPropertyName("patientId")]
    public Guid? PatientId { get; set; }

    [JsonPropertyName("doctorId")]
    public Guid? DoctorId { get; set; }

    [JsonPropertyName("slotTime")]
    public DateTime? SlotTime { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("bookingReferenceId")]
    public string? BookingReferenceId { get; set; }
}

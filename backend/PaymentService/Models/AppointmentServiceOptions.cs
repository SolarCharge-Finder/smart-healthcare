namespace PaymentService.Models;

public class AppointmentServiceOptions
{
    public const string SectionName = "AppointmentService";

    public string BaseUrl { get; set; } = "http://appointment-service:8080/";
}

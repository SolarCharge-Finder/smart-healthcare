namespace NotificationService.Configuration;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMQ";

    public string Host { get; set; } = "rabbitmq";
    public string? User { get; set; }
    public string? Password { get; set; }
}

namespace NotificationService.Configuration;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "auth-service";
    public string Audience { get; set; } = "smart-healthcare";
}

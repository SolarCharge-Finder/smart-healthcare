namespace Auth.Application.DTOs;

using Auth.Domain.Enums;
using System.Text.Json.Serialization;

public class RegisterRequest
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public UserRole Role { get; set; } = UserRole.Undefined;
}
namespace Auth.Application.DTOs;

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

using Auth.Domain.Enums;

public class RegisterRequest
{
    public string Name { get; set; } = "";

    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";
    [Required]
    [MinLength(6)]
    public string Password { get; set; } = "";

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public UserRole Role { get; set; } = UserRole.Undefined;
}

using System.ComponentModel.DataAnnotations;

namespace Auth.Application.DTOs;

public class ForgotPasswordRequest
{
    [Required]
    public string Email { get; set; } = string.Empty;
}
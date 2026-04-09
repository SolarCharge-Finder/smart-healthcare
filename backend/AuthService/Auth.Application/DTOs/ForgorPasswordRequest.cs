using System.ComponentModel.DataAnnotations;

namespace Auth.Application.DTOs;

public class ForgotPasswordRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
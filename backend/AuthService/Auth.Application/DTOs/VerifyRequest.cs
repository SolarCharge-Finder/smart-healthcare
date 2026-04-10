using System.ComponentModel.DataAnnotations;

namespace Auth.Application.DTOs;

public class VerifyRequest
{
    [Required]
    public string Token { get; set; } = string.Empty;
}

using Shared.Contracts.Enums;

namespace Auth.Application.DTOs;

public class SetRole
{
    public UserRole Role { get; set; } = UserRole.Patient;
}

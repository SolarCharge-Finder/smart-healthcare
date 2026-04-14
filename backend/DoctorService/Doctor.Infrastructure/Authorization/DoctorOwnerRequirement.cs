using Microsoft.AspNetCore.Authorization;

namespace Doctor.Infrastructure.Authorization;

public class DoctorOwnerRequirement : IAuthorizationRequirement
{
}

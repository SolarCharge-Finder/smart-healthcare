using Doctor.Application.Interfaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Doctor.Infrastructure.Authorization;

public class DoctorOwnerHandler : AuthorizationHandler<DoctorOwnerRequirement>
{
    private readonly IDoctorRepository _doctorRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DoctorOwnerHandler(
        IDoctorRepository doctorRepository,
        IHttpContextAccessor httpContextAccessor)
    {
        _doctorRepository = doctorRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DoctorOwnerRequirement requirement)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext == null)
        {
            return;
        }

        var userIdClaim = context.User.FindFirst("sub")?.Value;

        if (userIdClaim == null)
        {
            return;
        }

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return;
        }

        if (!httpContext.Request.RouteValues.TryGetValue("doctorId", out var doctorIdObj))
        {
            return;
        }

        if (!Guid.TryParse(doctorIdObj?.ToString(), out var doctorId))
        {
            return;
        }

        var doctor = await _doctorRepository.GetByIdAsync(doctorId);

        if (doctor != null && doctor.UserId == userId)
        {
            context.Succeed(requirement);
        }
    }
}

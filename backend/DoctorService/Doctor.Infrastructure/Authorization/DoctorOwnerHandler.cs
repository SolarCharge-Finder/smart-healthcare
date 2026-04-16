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
            context.Fail();
            return;
        }

        var userIdClaim =
            context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? context.User.FindFirst("sub")?.Value;

        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            context.Fail();
            return;
        }

        if (!httpContext.Request.RouteValues.TryGetValue("doctorId", out var doctorIdObj) ||
            !Guid.TryParse(doctorIdObj?.ToString(), out var doctorId))
        {
            context.Fail();
            return;
        }

        var doctor = await _doctorRepository.GetByIdAsync(doctorId);

        if (doctor == null || doctor.UserId != userId)
        {
            context.Fail();
            return;
        }

        context.Succeed(requirement);
    }
}

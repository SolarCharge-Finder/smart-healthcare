using System.Security.Claims;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DoctorService.Tests;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string UserIdHeader = "x-user-id";
    public const string RoleHeader = "x-user-role";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // read userId from header (fallback to random if not provided)
        var userId = Request.Headers[UserIdHeader].FirstOrDefault();

        if (string.IsNullOrEmpty(userId))
        {
            throw new Exception("x-user-id header is required in tests");
        }
        
        // read role from header (default to Admin to keep existing tests working)
        var role = Request.Headers[RoleHeader].FirstOrDefault()
                   ?? "Admin";

        var claims = new[]
        {
            new Claim("sub", userId), // for AuthServiceClient
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, $"{userId}@test.com"),
            new Claim(ClaimTypes.Role, role)
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
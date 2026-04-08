using Auth.Application.Interfaces;
using Auth.Application.Services;
using Auth.Infrastructure.Repositories;
using Auth.Infrastructure.Services;

namespace Auth.API.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITokenService, TokenService>();

        return services;
    }
}
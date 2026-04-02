namespace UserService.Infrastructure.Extensions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UserService.Application.Interfaces;
using UserService.Infrastructure.Data;
using UserService.Infrastructure.Repositories;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration config)
    {
        var host = config["DB_HOST"] ?? "localhost";
        var port = config["DB_PORT"] ?? "5432";
        var db = config["DB_NAME"] ?? "user-db";
        var user = config["DB_USER"] ?? "change-me";
        var pass = config["DB_PASSWORD"] ?? "change-me";

        var connectionString =
            $"Host={host};Port={port};Database={db};Username={user};Password={pass}";

        services.AddDbContext<UserDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }
}
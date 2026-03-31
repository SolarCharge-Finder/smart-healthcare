namespace Doctor.API.Extensions;

using Doctor.Application.Interfaces;
using Doctor.Application.Services;
using Doctor.Infrastructure.Data;
using Doctor.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;

public static class ServiceExtensions
{
    public static void AddApplicationServices(this IServiceCollection services, IConfiguration config)
    {
        var host = config["DB_HOST"] ?? "localhost";
        var port = config["DB_PORT"] ?? "5432";
        var db = config["DB_NAME"] ?? "doctordb";
        var user = config["DB_USER"] ?? "change-me";
        var pass = config["DB_PASSWORD"] ?? "change-me";

        var connectionString = $"Host={host};Port={port};Database={db};Username={user};Password={pass}";

        services.AddDbContext<DoctorDbContext>(options =>
            options.UseNpgsql(connectionString));


        services.AddScoped<IDoctorService, DoctorService>();
        services.AddScoped<IDoctorRepository, DoctorRepository>();
    }

    public static void AddApiServices(this IServiceCollection services)
    {
        services.AddControllers();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
    }
}
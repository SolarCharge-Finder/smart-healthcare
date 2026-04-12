namespace Doctor.API.Extensions;

using System.Text;

using Doctor.Application.Interfaces;
using Doctor.Application.Services;
using Doctor.Infrastructure.Authorization;
using Doctor.Infrastructure.Data;
using Doctor.Infrastructure.Repositories;
using Shared.Contracts.Infrastructure.Auth;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

public static class ServiceExtensions
{
    public static void AddApplicationServices(this IServiceCollection services, IConfiguration config)
    {
        var host = config["DB_HOST"] ?? "localhost";
        var port = config["DB_PORT"] ?? "5432";
        var db = config["DB_NAME"] ?? "doctordb";
        var user = config["DB_USER"] ?? "change-me";
        var pass = config["DB_PASSWORD"] ?? "change-me";
        var authServiceUrl = config["AUTH_SERVICE_URL"] ?? "http://auth-service";

        var connectionString = $"Host={host};Port={port};Database={db};Username={user};Password={pass}";

        services.AddDbContext<DoctorDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddHttpContextAccessor();

        services.AddScoped<IDoctorService, DoctorService>();
        services.AddScoped<IAvailabilityService, AvailabilityService>();
        services.AddScoped<IAvailabilityRepository, AvailabilityRepository>();
        services.AddScoped<IDoctorRepository, DoctorRepository>();
        services.AddScoped<IAuthorizationHandler, DoctorOwnerHandler>();
        services.AddHttpClient<IAuthServiceClient, AuthServiceClient>(client =>
        {
            client.BaseAddress = new Uri(authServiceUrl);
        });
    }



    public static void AddApiServices(this IServiceCollection services)
    {
        services.AddControllers();

        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter: Bearer YOUR_TOKEN"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    new string[] {}
                }
            });
        });
    }

    public static void AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("DoctorOwner", policy =>
            {
                policy.RequireRole("Doctor");
                policy.AddRequirements(new DoctorOwnerRequirement());
            });
        });
    }

    public static void AddJwtAuth(this IServiceCollection services, IConfiguration config)
    {
        var key = config["Jwt:Key"]!;

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,

                    ValidIssuer = config["Jwt:Issuer"],
                    ValidAudience = config["Jwt:Audience"],

                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(key)
                    )
                };
            });
    }
}

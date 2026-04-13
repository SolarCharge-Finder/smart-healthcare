using System.Text;
using System.Text.Json.Serialization;

using Auth.Application.Interfaces;
using Auth.Application.Services;
using Auth.Infrastructure.Data;
using Auth.Infrastructure.Repositories;
using Auth.Infrastructure.Services;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace Auth.API.Extensions;

public static class ServiceExtensions
{
    // application layer - services
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }

    // infrastructure layer - DB + repositories
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration config)
    {
        var host = config["DB_HOST"] ?? "localhost";
        var port = config["DB_PORT"] ?? "5432";
        var db = config["DB_NAME"] ?? "authdb";
        var user = config["DB_USER"] ?? "change-me";
        var pass = config["DB_PASSWORD"] ?? "change-me";

        var connectionString = $"Host={host};Port={port};Database={db};Username={user};Password={pass}";

        services.AddDbContext<AuthDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IUserRepository, UserRepository>();

        services.AddScoped<ITokenService, TokenService>();

        services.AddScoped<IEmailService, EmailService>();

        services.AddScoped<IVerificationService, VerificationService>();

        services.AddScoped<IUserService, UserService>();

        return services;
    }

    // api layer - controllers + swagger 
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(
                    new JsonStringEnumConverter());
            });

        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            // JWT support in Swagger
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
                    Array.Empty<string>()
                }
            });
        });

        // cors - allow frontend to call API
        services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                policy
                    .WithOrigins("http://localhost:3000")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return services;
    }

    public static void AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("InternalService", policy =>
            {
                policy.RequireClaim("scope", "internal");
                policy.RequireClaim("service", "doctor-service");
            });
        });
    }

    // Auth setup
    public static IServiceCollection AddJwtAuth(this IServiceCollection services, IConfiguration config)
    {
        var key = config["Jwt:Key"] ?? throw new Exception("JWT key not configured");

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

        return services;
    }
}

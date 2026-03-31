using Auth.Application.Interfaces;
using Auth.Application.Services;
using Auth.Infrastructure.Data;
using Auth.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// 🔹 DB connection
var host = builder.Configuration["DB_HOST"] ?? "localhost";
var port = builder.Configuration["DB_PORT"] ?? "5432";
var db = builder.Configuration["DB_NAME"] ?? "authdb";
var user = builder.Configuration["DB_USER"] ?? "postgres";
var pass = builder.Configuration["DB_PASSWORD"] ?? "admin";

var connectionString = $"Host={host};Port={port};Database={db};Username={user};Password={pass}";

// 🔹 Controllers
builder.Services.AddControllers();

// 🔹 Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 🔹 DbContext (Infrastructure)
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(connectionString));

// 🔹 Dependency Injection (IMPORTANT CHANGE)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// 🔹 JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtKey = builder.Configuration["Jwt:Key"]
            ?? throw new Exception("JWT Key is missing");

        var key = Encoding.UTF8.GetBytes(jwtKey);

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// 🔹 Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 🔹 Middleware
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseHttpMetrics();

app.UseAuthentication();
app.UseAuthorization();

// 🔹 Metrics endpoint
app.MapMetrics("/metrics/prometheus");

// 🔹 Controllers
app.MapControllers();

app.Run();

public partial class Program { }
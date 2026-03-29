using AuthService.Data;
using AuthService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

var host = builder.Configuration["DB_HOST"] ?? "localhost";
var port = builder.Configuration["DB_PORT"] ?? "5432";
var db = builder.Configuration["DB_NAME"] ?? "authdb";
var user = builder.Configuration["DB_USER"] ?? "postgres";
var pass = builder.Configuration["DB_PASSWORD"] ?? "admin";

var connectionString = $"Host={host};Port={port};Database={db};Username={user};Password={pass}";

// add controllers
builder.Services.AddControllers();

// swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// db
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(connectionString)
);

// services
builder.Services.AddScoped<AuthServiceLogic>();

// jwt authentication
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

// swagger ui
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// middleware order 
// redirect to http 
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// metrics middleware
app.UseHttpMetrics();

app.UseAuthentication();   //first authenticate
app.UseAuthorization();  

//metric endpoint
app.MapMetrics("/metrics/prometheus");

// map controllers
app.MapControllers();

app.Run();
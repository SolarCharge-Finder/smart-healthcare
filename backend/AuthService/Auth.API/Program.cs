using Auth.API.Extensions;
using Auth.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter());
    });

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DB
var connectionString = $"Host={builder.Configuration["DB_HOST"] ?? "localhost"};" +
                       $"Port={builder.Configuration["DB_PORT"] ?? "5432"};" +
                       $"Database={builder.Configuration["DB_NAME"] ?? "authdb"};" +
                       $"Username={builder.Configuration["DB_USER"] ?? "postgres"};" +
                       $"Password={builder.Configuration["DB_PASSWORD"] ?? "admin"}";

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(connectionString));

// Clean DI
builder.Services.AddApplicationServices();
builder.Services.AddJwtAuth(builder.Configuration);

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHttpsRedirection();
}

app.UseHttpMetrics();
app.UseAuthentication();
app.UseAuthorization();

app.MapMetrics("/metrics/prometheus");
app.MapControllers();

app.Run();

public partial class Program { }
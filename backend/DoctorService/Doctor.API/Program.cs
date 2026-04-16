using Doctor.API.Extensions;
using Doctor.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// register services
builder.Services.AddApplicationServices(builder.Configuration); //ServiceExtension

// controllers + swagger
builder.Services.AddApiServices(); //ServiceExtension

// JWT auth
builder.Services.AddJwtAuth(builder.Configuration);

// authorizations
builder.Services.AddAuthorizationPolicies();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<DoctorDbContext>();
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Doctor DB migration failed: {ex.Message}");
    }
}

// middleware
app.UseApiMiddleware();

// health check
app.MapGet("/health", () => Results.Ok("Healthy"));


app.Run();

public partial class Program { }

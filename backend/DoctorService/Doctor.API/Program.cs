using Doctor.API.Extensions;
using Doctor.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// register services
builder.Services.AddApplicationServices(builder.Configuration);

// controllers + swagger
builder.Services.AddApiServices();

// JWT auth
builder.Services.AddJwtAuth(builder.Configuration);

// authorization
builder.Services.AddAuthorization();

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

app.Run();

public partial class Program { }
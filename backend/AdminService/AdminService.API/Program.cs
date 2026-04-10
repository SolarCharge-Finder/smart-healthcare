using AdminService.API.Extensions;
using AdminService.Infrastructure.Data;
using AdminService.Infrastructure.Extensions;

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

// for accessing JWT in services
builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        Console.WriteLine("DB migration failed: " + ex.Message);
    }
}

// middleware
app.UseApiMiddleware();

// health check
app.MapGet("/health", () => Results.Ok("Healthy"));

app.Run();

public partial class Program { }

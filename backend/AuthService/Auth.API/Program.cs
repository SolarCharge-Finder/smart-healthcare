using Auth.API.Extensions;
using Auth.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplicationServices() // register services 
    .AddInfrastructureServices(builder.Configuration) // for accessing JWT in services
    .AddApiServices() // controllers + swagger
    .AddJwtAuth(builder.Configuration); // JWT auth

builder.Services.AddAuthorization(); // authorization

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
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

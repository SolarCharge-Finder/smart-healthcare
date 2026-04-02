using UserService.API.Extensions;
using UserService.Infrastructure.Data;
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
        var db = scope.ServiceProvider.GetRequiredService<UserServiceDbContext>();
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
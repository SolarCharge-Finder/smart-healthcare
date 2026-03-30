using AIService.Data;
using AIService.Middleware;
using AIService.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("serviceName", "ai-service")
    .Enrich.WithProperty("environment", builder.Environment.EnvironmentName)
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<AiDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

// OpenAI Service
builder.Services.AddSingleton<IOpenAIService, OpenAIService>();

// Validation and Parsing Services
builder.Services.AddSingleton<IValidationService, ValidationService>();
builder.Services.AddSingleton<IResponseParsingService, ResponseParsingService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AiDbContext>();
    
    if (app.Environment.IsEnvironment("Testing"))
    {
        db.Database.EnsureCreated();
        await AiDbContextSeed.SeedAsync(db);
    }
    else
    {
        db.Database.Migrate();
        await AiDbContextSeed.SeedAsync(db);
    }
}

app.UseHttpsRedirection();

app.UseCors("frontend");

// Custom middleware for correlation ID
app.UseMiddleware<CorrelationIdMiddleware>();

app.UseAuthorization();

app.MapControllers();

// Health endpoints
app.MapGet("/health/live", () => Results.Ok("alive"));

app.MapGet("/health/ready", async (AiDbContext db) =>
{
    var canConnect = await db.Database.CanConnectAsync();
    return canConnect ? Results.Ok("ready") : Results.StatusCode(503);
});

try
{
    Log.Information("Starting AI Service");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "AI Service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

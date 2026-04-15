using AIService.Data;
using AIService.Events;
using AIService.Middleware;
using AIService.Services;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using Serilog;
using StackExchange.Redis;

// Load .env file for local development (if it exists)
// Check multiple possible locations
var envPaths = new[]
{
    Path.Combine(Directory.GetCurrentDirectory(), ".env"),
    Path.Combine(AppContext.BaseDirectory, ".env"),
    Path.Combine(AppContext.BaseDirectory, "../../../.env"),  // If running from bin/Debug
};

foreach (var envPath in envPaths)
{
    if (File.Exists(envPath))
    {
        Console.WriteLine($"Loading .env from: {envPath}");
        foreach (var line in File.ReadAllLines(envPath))
        {
            // Skip empty lines and comments
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                continue;

            var parts = line.Split('=', 2);
            if (parts.Length == 2)
            {
                var key = parts[0].Trim();
                var value = parts[1].Trim();
                Environment.SetEnvironmentVariable(key, value);
                Console.WriteLine($"  ✓ Set: {key}");
            }
        }
        break;
    }
}

// Verify API key is set
var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
if (string.IsNullOrEmpty(apiKey))
{
    Console.WriteLine("⚠️  WARNING: OPENAI_API_KEY not found in .env or environment variables");
    Console.WriteLine($"   Current directory: {Directory.GetCurrentDirectory()}");
    Console.WriteLine($"   App base directory: {AppContext.BaseDirectory}");
}
else
{
    Console.WriteLine($"✓ OPENAI_API_KEY loaded ({apiKey.Substring(0, 10)}...)");
}

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
var useInMemoryDb = builder.Environment.IsEnvironment("Testing") ||
                    builder.Configuration.GetValue<bool>("Database:UseInMemory");

builder.Services.AddDbContext<AiDbContext>(options =>
{
    if (useInMemoryDb)
    {
        options.UseInMemoryDatabase("AiServiceTestDb");
    }
    else
    {
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
    }
});

// Secrets Management Service (must be before OpenAI Service)
builder.Services.AddSingleton<ISecretsService, SecretsService>();

var infrastructureStatus = new InfrastructureStatusService();
builder.Services.AddSingleton<IInfrastructureStatusService>(infrastructureStatus);

// Redis Connection for Caching
var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
try
{
    var redis = ConnectionMultiplexer.Connect(redisConnection);
    builder.Services.AddSingleton<IConnectionMultiplexer>(redis);
    builder.Services.AddSingleton<ICachingService, CachingService>();
    infrastructureStatus.SetRedisStatus(true);
    Console.WriteLine($"✓ Redis cache connected: {redisConnection.Split(':')[0]}:6379");
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️  Redis connection failed ({redisConnection}): {ex.Message}");
    Console.WriteLine("   Caching will be disabled but service will continue");
    infrastructureStatus.SetRedisStatus(false);
    builder.Services.AddSingleton<ICachingService>(new NoOpCachingService());
}

// RabbitMQ startup connectivity status
try
{
    var rabbitPort = int.TryParse(builder.Configuration["RabbitMQ:Port"], out var parsedPort) ? parsedPort : 5672;
    var rabbitFactory = new ConnectionFactory
    {
        HostName = builder.Configuration["RabbitMQ:Host"] ?? "localhost",
        UserName = builder.Configuration["RabbitMQ:User"] ?? "guest",
        Password = builder.Configuration["RabbitMQ:Password"] ?? "guest",
        Port = rabbitPort
    };

    await using var rabbitConnection = await rabbitFactory.CreateConnectionAsync();
    infrastructureStatus.SetRabbitMqStatus(true);
    Console.WriteLine($"✓ RabbitMQ connected: {rabbitFactory.HostName}:{rabbitFactory.Port}");
}
catch (Exception ex)
{
    infrastructureStatus.SetRabbitMqStatus(false);
    Console.WriteLine($"⚠️  RabbitMQ connection failed: {ex.Message}");
    Console.WriteLine("   Event publishing will use retry + dead-letter fallback");
}

// Fallback Service
builder.Services.AddSingleton<IFallbackService, FallbackService>();

// Event Publisher (for microservices integration)
builder.Services.AddSingleton<IEventPublisher, EventPublisher>();

// OpenAI Service
builder.Services.AddSingleton<IOpenAIService, OpenAIService>();

// Validation and Parsing Services
builder.Services.AddSingleton<IValidationService, ValidationService>();
builder.Services.AddSingleton<IResponseParsingService, ResponseParsingService>();
builder.Services.AddSingleton<IJsonResponseValidator, JsonResponseValidator>();

// Prompt Engineering Services
builder.Services.AddSingleton<IPromptService, PromptService>();

// Rate Limiting Service
builder.Services.AddSingleton<IRateLimitService, InMemoryRateLimitService>();

// Audit Logging Service
builder.Services.AddScoped<IAuditLoggingService, AuditLoggingService>();

// Specialty Mapping Service
builder.Services.AddSingleton<ISpecialtyMappingService, SpecialtyMappingService>();

// Confidence Scoring Service
builder.Services.AddScoped<IConfidenceScoringService, ConfidenceScoringService>();

// Urgency Classification Service
builder.Services.AddSingleton<IUrgencyClassificationService, UrgencyClassificationService>();

// Resilience Service (Circuit Breaker & Retry Policies)
builder.Services.AddSingleton<IResilienceService, ResilienceService>();

// Metrics Service (Prometheus)
builder.Services.AddSingleton<IMetricsService, MetricsService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:3000", "http://localhost:3001", "http://localhost:8081" };

        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
var swaggerEnabled = app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Swagger:Enabled");
if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AiDbContext>();

    if (useInMemoryDb)
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

// Rate Limiting Middleware (must be before authorization)
app.UseRateLimiting();

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

app.MapGet("/health/dependencies", (IInfrastructureStatusService statusService) =>
{
    return Results.Ok(statusService.GetSummary());
});

// Prometheus metrics endpoint
app.MapGet("/metrics", (IMetricsService metricsService) =>
{
    return Results.Text(metricsService.GetMetricsText(), "text/plain; version=0.0.4");
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

public partial class Program { }

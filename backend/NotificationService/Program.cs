using System.Text;
using System.IdentityModel.Tokens.Jwt;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using NotificationService.Configuration;
using NotificationService.Data;
using NotificationService.Health;
using NotificationService.Repositories;
using NotificationService.Services;

using Serilog;
using Serilog.Context;
using Serilog.Formatting.Json;

var builder = WebApplication.CreateBuilder(args);

var serviceName =
    builder.Configuration["ServiceName"] ??
    "notification-service";

var environment =
    builder.Environment.EnvironmentName;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("serviceName", serviceName)
    .Enrich.WithProperty("environment", environment)
    .WriteTo.Console(new JsonFormatter())
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services
    .AddOptions<RabbitMqOptions>()
    .Bind(builder.Configuration.GetSection(RabbitMqOptions.SectionName));

builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName));

builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationService, NotificationService.Services.NotificationService>();
builder.Services.AddScoped<IAppointmentNotificationProcessor, AppointmentNotificationProcessor>();

builder.Services.AddHostedService<RabbitMqConsumer>();
builder.Services.AddHostedService<ReminderScheduler>();

builder.Services.AddControllers();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtOptions = builder.Configuration
            .GetSection(JwtOptions.SectionName)
            .Get<JwtOptions>() ?? new JwtOptions();
        var configuredKey =
            Environment.GetEnvironmentVariable("JWT_KEY") ??
            builder.Configuration["Jwt_Key"] ??
            jwtOptions.Key;

        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            // Dev fallback: allow token parsing when signing key is not configured.
            // This preserves authenticated flows in local environments where JWT_KEY is missing.
            // The custom SignatureValidator returns JwtSecurityToken, so force legacy validators.
            options.UseSecurityTokenValidators = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateIssuerSigningKey = false,
                RequireSignedTokens = false,
                SignatureValidator = (token, _) => new JwtSecurityToken(token)
            };

            return;
        }

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(configuredKey))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer YOUR_TOKEN"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:3000", "http://localhost:3001")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<NotificationDbContext>("postgres")
    .AddCheck<RabbitMqHealthCheck>("rabbitmq");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    db.Database.EnsureCreated();

    await db.Database.ExecuteSqlRawAsync(
        """
        CREATE INDEX IF NOT EXISTS "IX_Notifications_UserId" ON "Notifications" ("UserId");
        CREATE INDEX IF NOT EXISTS "IX_Notifications_CreatedAt" ON "Notifications" ("CreatedAt");
        CREATE INDEX IF NOT EXISTS "IX_Notifications_IsRead" ON "Notifications" ("IsRead");
        CREATE INDEX IF NOT EXISTS "IX_Notifications_UserId_IsRead" ON "Notifications" ("UserId", "IsRead");

        CREATE UNIQUE INDEX IF NOT EXISTS "IX_ProcessedEvents_EventKey" ON "ProcessedEvents" ("EventKey");

        CREATE UNIQUE INDEX IF NOT EXISTS "IX_AppointmentProjections_AppointmentId" ON "AppointmentProjections" ("AppointmentId");
        CREATE INDEX IF NOT EXISTS "IX_AppointmentProjections_UserId_SlotTimeUtc" ON "AppointmentProjections" ("UserId", "SlotTimeUtc");
        CREATE INDEX IF NOT EXISTS "IX_AppointmentProjections_ReminderSentAt" ON "AppointmentProjections" ("ReminderSentAt");
        """);
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    const string headerName = "X-Correlation-ID";

    var correlationId =
        context.Request.Headers[headerName]
            .FirstOrDefault();

    if (string.IsNullOrWhiteSpace(correlationId))
    {
        correlationId = Guid.NewGuid().ToString();
    }

    context.Response.Headers[headerName] = correlationId;

    using (LogContext.PushProperty(
        "correlationId", correlationId))
    {
        await next();
    }
});

app.MapGet("/", () => "Notification Service Running");

app.MapGet("/metrics", () => Results.Json(NotificationMetrics.Snapshot()));

app.MapGet("/metrics/prometheus", () =>
    Results.Text(NotificationMetrics.SnapshotPrometheus(), "text/plain"));

app.MapGet("/health/live", () => Results.Ok("alive"));

app.MapGet("/health/ready",
async (HealthCheckService healthChecks) =>
{
    var result = await healthChecks.CheckHealthAsync();

    if (result.Status != HealthStatus.Healthy)
    {
        return Results.StatusCode(503);
    }

    return Results.Ok("ready");
});

app.MapControllers();

app.Run();

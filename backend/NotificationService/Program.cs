using Microsoft.Extensions.Diagnostics.HealthChecks;
using NotificationService.Health;
using NotificationService.Services;
using Serilog;
using Serilog.Formatting.Json;
using Serilog.Context;

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

builder.Services.AddHostedService<RabbitMqConsumer>();
builder.Services
    .AddHealthChecks()
    .AddCheck<RabbitMqHealthCheck>("rabbitmq");

var app = builder.Build();

app.MapGet("/", () => "Notification Service Running");

app.MapGet("/metrics", () =>
{
    return Results.Json(Metrics.Snapshot());
});

app.MapGet("/metrics/prometheus", () =>
{
    return Results.Text(
        Metrics.SnapshotPrometheus(),
        "text/plain");
});

app.Use(async (context, next) =>
{
    const string headerName = "X-Correlation-ID";

    var correlationId =
        context.Request.Headers[headerName]
            .FirstOrDefault();

    if (string.IsNullOrWhiteSpace(correlationId))
        correlationId = Guid.NewGuid().ToString();

    context.Response.Headers[headerName] = correlationId;
    context.Items["CorrelationId"] = correlationId;

    using (LogContext.PushProperty(
        "correlationId", correlationId))
    {
        await next();
    }
});

app.MapGet("/health/live", () =>
{
    return Results.Ok("alive");
});

app.MapGet("/health/ready",
async (HealthCheckService healthChecks) =>
{
    var result = await healthChecks.CheckHealthAsync();

    if (result.Status != HealthStatus.Healthy)
        return Results.StatusCode(503);

    return Results.Ok("ready");
});

app.Run();

static class Metrics
{
    private static long _appointmentsCreatedTotal;
    private static long _appointmentsCancelledTotal;
    private static long _appointmentsConflictTotal;
    private static long _rabbitMqPublishSuccessTotal;
    private static long _rabbitMqPublishFailureTotal;

    public static object Snapshot()
    {
        return new
        {
            appointments_created_total =
                Interlocked.Read(ref _appointmentsCreatedTotal),
            appointments_cancelled_total =
                Interlocked.Read(ref _appointmentsCancelledTotal),
            appointments_conflict_total =
                Interlocked.Read(ref _appointmentsConflictTotal),
            rabbitmq_publish_success_total =
                Interlocked.Read(ref _rabbitMqPublishSuccessTotal),
            rabbitmq_publish_failure_total =
                Interlocked.Read(ref _rabbitMqPublishFailureTotal)
        };
    }

    public static string SnapshotPrometheus()
    {
        return
            $"appointments_created_total {Interlocked.Read(ref _appointmentsCreatedTotal)}\n" +
            $"appointments_cancelled_total {Interlocked.Read(ref _appointmentsCancelledTotal)}\n" +
            $"appointments_conflict_total {Interlocked.Read(ref _appointmentsConflictTotal)}\n" +
            $"rabbitmq_publish_success_total {Interlocked.Read(ref _rabbitMqPublishSuccessTotal)}\n" +
            $"rabbitmq_publish_failure_total {Interlocked.Read(ref _rabbitMqPublishFailureTotal)}\n";
    }
}

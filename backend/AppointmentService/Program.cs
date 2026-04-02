using AppointmentService.Data;
using AppointmentService.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using AppointmentService.Models;
using AppointmentService.Messaging;
using System.Data;
using Npgsql;
using Serilog;
using Serilog.Formatting.Json;
using Serilog.Context;
using StackExchange.Redis;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

var serviceName =
    builder.Configuration["ServiceName"] ??
    "appointment-service";

var environment =
    builder.Environment.EnvironmentName;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("serviceName", serviceName)
    .Enrich.WithProperty("environment", environment)
    .WriteTo.Console(new JsonFormatter())
    .CreateLogger();

builder.Host.UseSerilog();

// swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var frontendOrigin =
    builder.Configuration["Frontend__Origin"];
var enableCors =
    builder.Environment.IsDevelopment() ||
    !string.IsNullOrWhiteSpace(frontendOrigin);

if (enableCors)
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("frontend", policy =>
        {
            if (builder.Environment.IsDevelopment() &&
                string.IsNullOrWhiteSpace(frontendOrigin))
            {
                policy.AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            }
            else if (!string.IsNullOrWhiteSpace(frontendOrigin))
            {
                policy.WithOrigins(frontendOrigin)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            }
        });
    });
}

// postgres
builder.Services.AddDbContext<AppointmentDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));


// rabbitmq publisher
builder.Services.AddSingleton<RabbitMqPublisher>();
builder.Services.AddSingleton<ICorrelationIdAccessor, CorrelationIdAccessor>();
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddHostedService<OutboxPublisher>();
}

var redisConnectionString =
    builder.Configuration["Redis:ConnectionString"];

if (string.IsNullOrWhiteSpace(redisConnectionString))
{
    var redisHost =
        builder.Configuration["Redis:Host"] ??
        "redis:6379";
    var redisPassword =
        builder.Configuration["Redis:Password"];

    redisConnectionString =
        string.IsNullOrWhiteSpace(redisPassword)
            ? redisHost
            : $"{redisHost},password={redisPassword}";
}

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(redisConnectionString));


var app = builder.Build();


// ensure db created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<AppointmentDbContext>();

    if (app.Environment.IsEnvironment("Testing"))
    {
        db.Database.EnsureCreated();
    }
    else
    {
        db.Database.Migrate();
    }
}


app.UseSwagger();
app.UseSwaggerUI();

if (enableCors)
{
    app.UseCors("frontend");
}

app.UseHttpsRedirection();

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

    var accessor = context.RequestServices
        .GetRequiredService<ICorrelationIdAccessor>();

    accessor.CorrelationId = correlationId;

    using (LogContext.PushProperty(
        "correlationId", correlationId))
    {
        try
        {
            await next();
        }
        finally
        {
            accessor.CorrelationId = null;
        }
    }
});

/*
health endpoints
*/

app.MapGet("/health/live", () =>
{
    return Results.Ok("alive");
});


app.MapGet("/health/ready",
async (AppointmentDbContext db) =>
{
    var canConnect = await db.Database.CanConnectAsync();

    if (!canConnect)
        return Results.StatusCode(503);

    return Results.Ok("ready");
});


/*
test db
*/

app.MapGet("/test-db",
async (AppointmentDbContext db) =>
{
    var count = await db.Appointments.CountAsync();

    return Results.Ok(new
    {
        totalAppointments = count
    });
});


/*
list appointments
*/

app.MapGet("/appointments",
async (AppointmentDbContext db) =>
{
    var appointments = await db.Appointments
        .AsNoTracking()
        .Select(a => new AppointmentDto
        {
            Id = a.Id,
            PatientId = a.PatientId,
            DoctorId = a.DoctorId,
            SlotTime = a.SlotTime,
            Status = a.Status,
            CreatedAt = a.CreatedAt
        })
        .ToListAsync();

    return Results.Ok(appointments);
});


/*
get appointment by id
*/

app.MapGet("/appointments/{id:guid}",
async (Guid id, AppointmentDbContext db) =>
{
    var appointment = await db.Appointments
        .AsNoTracking()
        .Where(a => a.Id == id)
        .Select(a => new AppointmentDto
        {
            Id = a.Id,
            PatientId = a.PatientId,
            DoctorId = a.DoctorId,
            SlotTime = a.SlotTime,
            Status = a.Status,
            CreatedAt = a.CreatedAt
        })
        .FirstOrDefaultAsync();

    if (appointment is null)
        return Results.NotFound();

    return Results.Ok(appointment);
});


/*
availability
*/

app.MapGet("/appointments/availability",
async (
    Guid doctorId,
    DateTime from,
    DateTime to,
    AppointmentDbContext db) =>
{
    if (from >= to)
        return Results.BadRequest(
            "`from` must be earlier than `to`"
        );

    if ((to - from).TotalDays > 30)
        return Results.BadRequest(
            "Range must not exceed 30 days"
        );

    var booked = await db.Appointments
        .AsNoTracking()
        .Where(a =>
            a.DoctorId == doctorId &&
            a.Status == "Booked" &&
            a.SlotTime >= from &&
            a.SlotTime <= to
        )
        .Select(a => a.SlotTime)
        .ToListAsync();

    var bookedSet = booked
        .ToHashSet();

    var available = new List<DateTime>();

    var cursor = from;
    var step = TimeSpan.FromMinutes(30);

    while (cursor <= to)
    {
        if (!bookedSet.Contains(cursor))
            available.Add(cursor);

        cursor = cursor.Add(step);
    }

    var availableIso = available
        .Select(a => a.ToString("O"))
        .ToList();

    return Results.Ok(availableIso);
});

/*
update appointment status to Paid after payment completion - ensure the telemedicine token - Sachithra
*/

RequireApiKey(
app.MapPatch("/appointments/{id:guid}/confirm-payment",
async (
    Guid id,
    AppointmentDbContext db,
    ICorrelationIdAccessor correlationAccessor,
    ILogger<Program> logger) =>
{
    var appointment = await db.Appointments
        .FirstOrDefaultAsync(a => a.Id == id);

    if (appointment is null)
        return Results.NotFound(new { error = "Appointment not found" });

    if (appointment.Status == "Paid")
        return Results.Ok(new { message = "Appointment already marked as Paid", appointment });

    if (appointment.Status == "Cancelled")
        return Results.BadRequest(new { error = "Cannot update cancelled appointment" });

    // Update appointment status to Paid after successful payment
    appointment.Status = "Paid";
    
    var paymentConfirmedEvent = new
    {
        appointment.Id,
        appointment.PatientId,
        appointment.DoctorId,
        appointment.SlotTime,
        Status = "Paid",
        PaidAt = DateTime.UtcNow
    };

    db.OutboxMessages.Add(
        CreateOutboxMessage(
            "appointment.payment-confirmed",
            paymentConfirmedEvent,
            correlationAccessor.CorrelationId));

    await db.SaveChangesAsync();

    logger.LogInformation(
        "Appointment {AppointmentId} marked as Paid after successful payment",
        appointment.Id);
    
    Metrics.IncAppointmentsPaid();

    return Results.Ok(new 
    { 
        message = "Appointment payment confirmed",
        appointmentId = appointment.Id,
        status = appointment.Status
    });
}));

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


/*
cancel appointment
*/

RequireApiKey(
app.MapDelete("/appointments/{id:guid}",
async (
    Guid id,
    AppointmentDbContext db,
    ICorrelationIdAccessor correlationAccessor,
    ILogger<Program> logger) =>
{
    var appointment = await db.Appointments
        .FirstOrDefaultAsync(a => a.Id == id);

    if (appointment is null)
        return Results.NotFound();

    if (appointment.Status == "Cancelled")
        return Results.BadRequest(
            "Appointment already cancelled"
        );

    if (appointment.SlotTime < DateTime.UtcNow)
        return Results.BadRequest(
            "Cannot cancel past appointment"
        );

    appointment.Status = "Cancelled";

    var cancelEvent = new
    {
        appointment.Id,
        appointment.PatientId,
        appointment.DoctorId,
        appointment.SlotTime
    };

    db.OutboxMessages.Add(
        CreateOutboxMessage(
            "appointment.cancelled",
            cancelEvent,
            correlationAccessor.CorrelationId));

    await db.SaveChangesAsync();

    logger.LogInformation(
        "Appointment cancelled {AppointmentId}",
        appointment.Id
    );
    Metrics.IncAppointmentsCancelled();

    return Results.Ok(appointment);
}));


/*
create appointment
*/

RequireApiKey(
app.MapPost("/appointments",

async (
    HttpContext http,
    Appointment appointment,
    AppointmentDbContext db,
    IConnectionMultiplexer redis,
    ICorrelationIdAccessor correlationAccessor,
    ILogger<Program> logger) =>
{
    const int maxRetries = 3;
    var idempotencyKey = http.Request.Headers["Idempotency-Key"]
        .FirstOrDefault();
    var redisLockUnavailable = false;

    if (string.IsNullOrWhiteSpace(idempotencyKey))
        idempotencyKey = null;

    var lockKey = BuildLockKey(appointment);

    for (var attempt = 1; attempt <= maxRetries; attempt++)
    {
        string? lockToken = null;
        IDbContextTransaction? tx = null;
        try
        {
            if (!redisLockUnavailable)
            {
                try
                {
                    lockToken = await TryAcquireRedisLockAsync(
                        redis,
                        lockKey,
                        TimeSpan.FromSeconds(10));
                }
                catch (RedisConnectionException ex)
                {
                    redisLockUnavailable = true;
                    logger.LogWarning(
                        ex,
                        "Redis lock unavailable while creating appointment. Falling back to database transaction only.");
                }
                catch (RedisTimeoutException ex)
                {
                    redisLockUnavailable = true;
                    logger.LogWarning(
                        ex,
                        "Redis lock timed out while creating appointment. Falling back to database transaction only.");
                }
            }

            if (!redisLockUnavailable && lockToken is null)
            {
                if (attempt == maxRetries)
                {
                    Metrics.IncAppointmentsConflict();
                    return Results.Conflict(
                        "Slot already booked"
                    );
                }

                var delayMs =
                    (int)(100 * Math.Pow(2, attempt - 1));
                await Task.Delay(delayMs);
                continue;
            }

            tx = await db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

            await db.Database.ExecuteSqlRawAsync(
                "SELECT pg_advisory_xact_lock(hashtext({0}))",
                lockKey);

            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                var existingRecord = await db.IdempotencyRecords
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r =>
                        r.Key == idempotencyKey);

                if (existingRecord is not null)
                {
                    var existingAppointment =
                        await db.Appointments
                            .AsNoTracking()
                            .FirstOrDefaultAsync(a =>
                                a.Id ==
                                existingRecord.AppointmentId);

                    return Results.Ok(existingAppointment);
                }
            }

            var exists =
                await db.Appointments.AnyAsync(a =>
                    a.DoctorId == appointment.DoctorId &&
                    a.SlotTime == appointment.SlotTime
                );

            if (exists)
            {
                logger.LogWarning(
                    "Booking conflict detected for doctor {DoctorId} at {SlotTime}",
                    appointment.DoctorId,
                    appointment.SlotTime
                );

                Metrics.IncAppointmentsConflict();
                return Results.Conflict(
                    "Slot already booked"
                );
            }

            appointment.Id = Guid.NewGuid();
            appointment.Status = "Booked";
            appointment.CreatedAt = DateTime.UtcNow;

            db.Appointments.Add(appointment);

            var createEvent = new
            {
                appointment.Id,
                appointment.PatientId,
                appointment.DoctorId,
                appointment.SlotTime
            };

            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                db.IdempotencyRecords.Add(
                    new IdempotencyRecord
                    {
                        Id = Guid.NewGuid(),
                        Key = idempotencyKey,
                        CreatedAt = DateTime.UtcNow,
                        AppointmentId = appointment.Id
                    });
            }

            db.OutboxMessages.Add(
                CreateOutboxMessage(
                    "appointment.created",
                    createEvent,
                    correlationAccessor.CorrelationId));

            await db.SaveChangesAsync();

            await tx.CommitAsync();

            logger.LogInformation(
                "Appointment created {AppointmentId}",
                appointment.Id
            );
            Metrics.IncAppointmentsCreated();

            return Results.Ok(appointment);
        }
        catch (Exception ex) when (IsUniqueConstraintViolation(ex))
        {
            if (tx is not null)
                await TryRollbackAsync(tx);

            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                var existingRecord = await db.IdempotencyRecords
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r =>
                        r.Key == idempotencyKey);

                if (existingRecord is not null)
                {
                    var existingAppointment =
                        await db.Appointments
                            .AsNoTracking()
                            .FirstOrDefaultAsync(a =>
                                a.Id ==
                                existingRecord.AppointmentId);

                    return Results.Ok(existingAppointment);
                }
            }

            logger.LogWarning(
                "Booking conflict detected for doctor {DoctorId} at {SlotTime}",
                appointment.DoctorId,
                appointment.SlotTime
            );
            Metrics.IncAppointmentsConflict();
            return Results.Conflict("Slot already booked");
        }
        catch (Exception ex) when (IsSerializationFailure(ex))
        {
            if (tx is not null)
                await TryRollbackAsync(tx);

            if (attempt == maxRetries)
            {
                logger.LogWarning(
                    "Booking conflict detected after retries for doctor {DoctorId} at {SlotTime}",
                    appointment.DoctorId,
                    appointment.SlotTime
                );
                Metrics.IncAppointmentsConflict();
                return Results.Conflict(
                    "Booking conflict, please retry"
                );
            }

            db.ChangeTracker.Clear();

            var delayMs = (int)(100 * Math.Pow(2, attempt - 1));
            await Task.Delay(delayMs);
        }
        finally
        {
            if (tx is not null)
            {
                await tx.DisposeAsync();
            }

            if (lockToken is not null)
            {
                await ReleaseRedisLockAsync(
                    redis,
                    lockKey,
                    lockToken);
            }
        }
    }

    Metrics.IncAppointmentsConflict();
    return Results.Conflict("Booking conflict, please retry");
}));


app.Run();

static RouteHandlerBuilder RequireApiKey(
    RouteHandlerBuilder builder)
{
    builder.AddEndpointFilter(async (context, next) =>
    {
        var http = context.HttpContext;
        var config = http.RequestServices
            .GetRequiredService<IConfiguration>();

        var expected = config["API_KEY"];
        var provided = http.Request.Headers["X-API-KEY"]
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(expected) ||
            string.IsNullOrWhiteSpace(provided) ||
            provided != expected)
            return Results.Unauthorized();

        return await next(context);
    });

    return builder;
}


static bool IsSerializationFailure(Exception ex)
{
    return HasPostgresSqlState(ex, "40001");
}


static bool IsUniqueConstraintViolation(Exception ex)
{
    return HasPostgresSqlState(ex, "23505");
}


static bool HasPostgresSqlState(Exception ex, string sqlState)
{
    if (ex is PostgresException pg && pg.SqlState == sqlState)
        return true;

    if (ex is DbUpdateException dbEx &&
        dbEx.InnerException is PostgresException pgInner &&
        pgInner.SqlState == sqlState)
        return true;

    return ex.InnerException is not null &&
           HasPostgresSqlState(ex.InnerException, sqlState);
}


static async Task TryRollbackAsync(
    IDbContextTransaction tx)
{
    try
    {
        await tx.RollbackAsync();
    }
    catch
    {
    }
}

static OutboxMessage CreateOutboxMessage(
    string eventName,
    object payload,
    string? correlationId)
{
    return new OutboxMessage
    {
        Id = Guid.NewGuid(),
        EventName = eventName,
        Payload = JsonSerializer.Serialize(payload),
        CorrelationId = correlationId ?? string.Empty,
        CreatedAt = DateTime.UtcNow
    };
}

static string BuildLockKey(Appointment appointment)
{
    return
        $"lock:appointment:{appointment.DoctorId}:{appointment.SlotTime:O}";
}

static async Task<string?> TryAcquireRedisLockAsync(
    IConnectionMultiplexer redis,
    string lockKey,
    TimeSpan ttl)
{
    var db = redis.GetDatabase();
    var token = Guid.NewGuid().ToString();

    var acquired = await db.StringSetAsync(
        lockKey,
        token,
        ttl,
        When.NotExists);

    return acquired ? token : null;
}

static async Task ReleaseRedisLockAsync(
    IConnectionMultiplexer redis,
    string lockKey,
    string token)
{
    const string script = @"
if redis.call('get', KEYS[1]) == ARGV[1] then
    return redis.call('del', KEYS[1])
end
return 0";

    var db = redis.GetDatabase();
    await db.ScriptEvaluateAsync(
        script,
        new RedisKey[] { lockKey },
        new RedisValue[] { token });
}

internal static class Metrics
{
    private static long _appointmentsCreatedTotal;
    private static long _appointmentsCancelledTotal;
    private static long _appointmentsPaidTotal;
    private static long _appointmentsConflictTotal;
    private static long _rabbitMqPublishSuccessTotal;
    private static long _rabbitMqPublishFailureTotal;

    public static void IncAppointmentsCreated() =>
        Interlocked.Increment(ref _appointmentsCreatedTotal);

    public static void IncAppointmentsCancelled() =>
        Interlocked.Increment(ref _appointmentsCancelledTotal);

    public static void IncAppointmentsPaid() =>
        Interlocked.Increment(ref _appointmentsPaidTotal);

    public static void IncAppointmentsConflict() =>
        Interlocked.Increment(ref _appointmentsConflictTotal);

    public static void IncRabbitMqPublishSuccess() =>
        Interlocked.Increment(ref _rabbitMqPublishSuccessTotal);

    public static void IncRabbitMqPublishFailure() =>
        Interlocked.Increment(ref _rabbitMqPublishFailureTotal);

    public static object Snapshot()
    {
        return new
        {
            appointments_created_total =
                Interlocked.Read(ref _appointmentsCreatedTotal),
            appointments_cancelled_total =
                Interlocked.Read(ref _appointmentsCancelledTotal),
            appointments_paid_total =
                Interlocked.Read(ref _appointmentsPaidTotal),
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
            $"appointments_paid_total {Interlocked.Read(ref _appointmentsPaidTotal)}\n" +
            $"appointments_conflict_total {Interlocked.Read(ref _appointmentsConflictTotal)}\n" +
            $"rabbitmq_publish_success_total {Interlocked.Read(ref _rabbitMqPublishSuccessTotal)}\n" +
            $"rabbitmq_publish_failure_total {Interlocked.Read(ref _rabbitMqPublishFailureTotal)}\n";
    }
}

public partial class Program { }

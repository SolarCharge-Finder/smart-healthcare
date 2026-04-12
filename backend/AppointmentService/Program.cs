using System.Data;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;

using AppointmentService.Data;
using AppointmentService.Logging;
using AppointmentService.Messaging;
using AppointmentService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

using Serilog;
using Serilog.Context;
using Serilog.Formatting.Json;
using StackExchange.Redis;

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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var frontendOrigin =
    builder.Configuration["Frontend:Origin"] ??
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

builder.Services.AddDbContext<AppointmentDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

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

    await EnsureDynamicSchemaAsync(db);
    await SeedDoctorAvailabilitiesAsync(db);
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
    {
        correlationId = Guid.NewGuid().ToString();
    }

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

app.MapGet("/health/live", () => Results.Ok("alive"));

app.MapGet("/health/ready",
async (AppointmentDbContext db) =>
{
    var canConnect = await db.Database.CanConnectAsync();

    if (!canConnect)
    {
        return Results.StatusCode(503);
    }

    return Results.Ok("ready");
});

app.MapGet("/test-db",
async (AppointmentDbContext db) =>
{
    var count = await db.Appointments.CountAsync();

    return Results.Ok(new
    {
        totalAppointments = count
    });
});

app.MapGet("/doctors",
async (AppointmentDbContext db) =>
{
    var doctors = await db.DoctorAvailabilities
        .AsNoTracking()
        .GroupBy(d => new
        {
            d.DoctorId,
            d.DoctorName,
            d.Specialization,
            d.HospitalId,
            d.HospitalName
        })
        .Select(group => new
        {
            doctorId = group.Key.DoctorId,
            doctorName = group.Key.DoctorName,
            specialization = group.Key.Specialization,
            hospitalId = group.Key.HospitalId,
            hospitalName = group.Key.HospitalName,
            isVideoConsultation = true,
            nextAvailableSlot = group
                .OrderBy(d => d.SlotTime)
                .Select(d => d.SlotTime)
                .FirstOrDefault(),
            doctorFee = group
                .OrderBy(d => d.SlotTime)
                .Select(d => d.DoctorFee)
                .FirstOrDefault(),
            hospitalFee = group
                .OrderBy(d => d.SlotTime)
                .Select(d => d.HospitalFee)
                .FirstOrDefault(),
            eChannellingFee = group
                .OrderBy(d => d.SlotTime)
                .Select(d => d.EChannellingFee)
                .FirstOrDefault(),
            discount = group
                .OrderBy(d => d.SlotTime)
                .Select(d => d.Discount)
                .FirstOrDefault(),
            totalFee = group
                .OrderBy(d => d.SlotTime)
                .Select(d => d.DoctorFee + d.HospitalFee + d.EChannellingFee - d.Discount)
                .FirstOrDefault()
        })
        .OrderBy(d => d.doctorName)
        .ThenBy(d => d.hospitalName)
        .ToListAsync();

    return Results.Ok(doctors);
});

app.MapGet("/doctors/filter-options",
async (AppointmentDbContext db, IConfiguration config) =>
{
    var doctorNames = await db.DoctorAvailabilities
        .AsNoTracking()
        .Select(d => d.DoctorName)
        .Distinct()
        .OrderBy(x => x)
        .ToListAsync();

    var specializations = await db.DoctorAvailabilities
        .AsNoTracking()
        .Select(d => d.Specialization)
        .Distinct()
        .OrderBy(x => x)
        .ToListAsync();

    var hospitals = await db.DoctorAvailabilities
        .AsNoTracking()
        .Select(d => new { d.HospitalId, d.HospitalName })
        .Distinct()
        .OrderBy(x => x.HospitalName)
        .ToListAsync();

    var approvedDoctors = await FetchApprovedDoctorsAsync(config);

    foreach (var doctor in approvedDoctors)
    {
        if (!string.IsNullOrWhiteSpace(doctor.FullName) &&
            !doctorNames.Contains(doctor.FullName))
        {
            doctorNames.Add(doctor.FullName);
        }

        if (!string.IsNullOrWhiteSpace(doctor.Specialization) &&
            !specializations.Contains(doctor.Specialization))
        {
            specializations.Add(doctor.Specialization);
        }

        if (!string.IsNullOrWhiteSpace(doctor.Hospital) &&
            !hospitals.Any(h => string.Equals(h.HospitalName, doctor.Hospital, StringComparison.OrdinalIgnoreCase)))
        {
            hospitals.Add(new
            {
                HospitalId = NormalizeHospitalId(doctor.Hospital),
                HospitalName = doctor.Hospital
            });
        }
    }

    doctorNames = doctorNames
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(x => x)
        .ToList();

    specializations = specializations
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(x => x)
        .ToList();

    hospitals = hospitals
        .DistinctBy(h => h.HospitalName, StringComparer.OrdinalIgnoreCase)
        .OrderBy(h => h.HospitalName)
        .ToList();

    return Results.Ok(new
    {
        doctorNames,
        specializations,
        hospitals = hospitals.Select(h => new
        {
            id = h.HospitalId,
            name = h.HospitalName
        })
    });
});

app.MapGet("/doctors/search",
async (
    string? doctorName,
    string? specialization,
    string? hospitalId,
    DateOnly date,
    AppointmentDbContext db) =>
{
    var nowUtc = DateTime.UtcNow;
    var today = DateOnly.FromDateTime(nowUtc.Date);
    if (date < today)
        return Results.BadRequest("Date must be today or in the future.");

    var dayStart = ToUtc(date.ToDateTime(TimeOnly.MinValue));
    var dayEnd = dayStart.AddDays(1);

    var query = db.DoctorAvailabilities
        .AsNoTracking()
        .Where(d =>
            d.SlotTime >= dayStart &&
            d.SlotTime < dayEnd);

    if (!string.IsNullOrWhiteSpace(doctorName))
        query = query.Where(d => d.DoctorName == doctorName);

    if (!string.IsNullOrWhiteSpace(specialization))
        query = query.Where(d => d.Specialization == specialization);

    if (!string.IsNullOrWhiteSpace(hospitalId))
        query = query.Where(d => d.HospitalId == hospitalId);

    if (date == today)
        query = query.Where(d => d.SlotTime >= nowUtc);

    var candidateSlots = await query
        .OrderBy(d => d.DoctorName)
        .ThenBy(d => d.SlotTime)
        .ToListAsync();

    if (candidateSlots.Count == 0)
        return Results.Ok(Array.Empty<object>());

    var doctorIds = candidateSlots
        .Select(s => s.DoctorId)
        .Distinct()
        .ToList();

    var bookedLookup = await db.Appointments
        .AsNoTracking()
        .Where(a =>
            doctorIds.Contains(a.DoctorId) &&
            a.SlotTime >= dayStart &&
            a.SlotTime < dayEnd &&
            a.Status != "CANCELLED" &&
            a.Status != "DECLINED")
        .Select(a => new { a.DoctorId, a.SlotTime })
        .ToListAsync();

    var bookedSet = bookedLookup
        .Select(x => $"{x.DoctorId}:{x.SlotTime:O}")
        .ToHashSet();

    var results = candidateSlots
        .GroupBy(s => new
        {
            s.DoctorId,
            s.DoctorName,
            s.Specialization,
            s.HospitalId,
            s.HospitalName
        })
        .Select(group =>
        {
            var slots = group
                .Where(s => !bookedSet.Contains($"{s.DoctorId}:{s.SlotTime:O}"))
                .Select(s => s.SlotTime)
                .OrderBy(t => t)
                .Select(t => t.ToString("HH:mm"))
                .Distinct()
                .ToList();

            var pricingSeed = group.First();
            var pricing = ToPricingResponse(pricingSeed);

            return new
            {
                doctorId = group.Key.DoctorId,
                doctorName = group.Key.DoctorName,
                specialization = group.Key.Specialization,
                hospitalId = group.Key.HospitalId,
                hospitalName = group.Key.HospitalName,
                date = date.ToString("yyyy-MM-dd"),
                availableSlots = slots,
                pricing
            };
        })
        .Where(r => r.availableSlots.Count > 0)
        .OrderBy(r => r.doctorName)
        .ToList();

    return Results.Ok(results);
});

app.MapGet("/doctors/availability",
async (
    Guid doctorId,
    DateOnly date,
    AppointmentDbContext db) =>
{
    var nowUtc = DateTime.UtcNow;
    var today = DateOnly.FromDateTime(nowUtc.Date);

    var dayStart = ToUtc(date.ToDateTime(TimeOnly.MinValue));
    var dayEnd = dayStart.AddDays(1);

    var slots = await db.DoctorAvailabilities
        .AsNoTracking()
        .Where(d =>
            d.DoctorId == doctorId &&
            d.SlotTime >= dayStart &&
            d.SlotTime < dayEnd)
        .Where(d => date != today || d.SlotTime >= nowUtc)
        .Select(d => d.SlotTime)
        .ToListAsync();

    var booked = await db.Appointments
        .AsNoTracking()
        .Where(a =>
            a.DoctorId == doctorId &&
            a.SlotTime >= dayStart &&
            a.SlotTime < dayEnd &&
            a.Status != "CANCELLED" &&
            a.Status != "DECLINED")
        .Select(a => a.SlotTime)
        .ToListAsync();

    var bookedSet = booked.ToHashSet();

    var available = slots
        .Where(s => !bookedSet.Contains(s))
        .OrderBy(s => s)
        .Select(s => s.ToString("HH:mm"))
        .ToList();

    return Results.Ok(new
    {
        doctorId,
        date = date.ToString("yyyy-MM-dd"),
        availableSlots = available
    });
});

app.MapGet("/appointments/pricing",
async (
    Guid doctorId,
    string? hospitalId,
    AppointmentDbContext db) =>
{
    var slot = await db.DoctorAvailabilities
        .AsNoTracking()
        .Where(d => d.DoctorId == doctorId)
        .Where(d => string.IsNullOrWhiteSpace(hospitalId) || d.HospitalId == hospitalId)
        .OrderBy(d => d.SlotTime)
        .FirstOrDefaultAsync();

    if (slot is null)
        return Results.NotFound("Pricing not found for selected doctor.");

    return Results.Ok(ToPricingResponse(slot));
});

app.MapGet("/appointments",
async (AppointmentDbContext db) =>
{
    var appointments = await db.Appointments
        .Include(a => a.GuestUser)
        .AsNoTracking()
        .OrderByDescending(a => a.CreatedAt)
        .Select(ToAppointmentDtoExpression())
        .ToListAsync();

    return Results.Ok(appointments);
});

app.MapGet("/appointments/{id:guid}",
async (Guid id, AppointmentDbContext db) =>
{
    var appointment = await db.Appointments
        .Include(a => a.GuestUser)
        .AsNoTracking()
        .Where(a => a.Id == id)
        .Select(ToAppointmentDtoExpression())
        .FirstOrDefaultAsync();

    if (appointment is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(appointment);
});

app.MapGet("/appointments/pending",
async (
    Guid? userId,
    string? guestEmail,
    AppointmentDbContext db) =>
{
    var query = db.Appointments
        .Include(a => a.GuestUser)
        .AsNoTracking()
        .Where(a => a.Status == "PENDING_PAYMENT");

    if (userId.HasValue)
        query = query.Where(a => a.UserId == userId.Value);

    if (!string.IsNullOrWhiteSpace(guestEmail))
        query = query.Where(a => a.GuestUser != null && a.GuestUser.Email == guestEmail);

    var items = await query
        .OrderByDescending(a => a.CreatedAt)
        .Select(ToAppointmentDtoExpression())
        .ToListAsync();

    return Results.Ok(items);
});

app.MapGet("/appointments/availability",
async (
    Guid doctorId,
    DateTime from,
    DateTime to,
    AppointmentDbContext db) =>
{
    var fromUtc = ToUtc(from);
    var toUtc = ToUtc(to);

    if (fromUtc >= toUtc)
        return Results.BadRequest("`from` must be earlier than `to`");

    if ((toUtc - fromUtc).TotalDays > 30)
        return Results.BadRequest("Range must not exceed 30 days");

    var slots = await db.DoctorAvailabilities
        .AsNoTracking()
        .Where(d =>
            d.DoctorId == doctorId &&
            d.SlotTime >= fromUtc &&
            d.SlotTime <= toUtc)
        .Select(d => d.SlotTime)
        .ToListAsync();

    var booked = await db.Appointments
        .AsNoTracking()
        .Where(a =>
            a.DoctorId == doctorId &&
            a.SlotTime >= fromUtc &&
            a.SlotTime <= toUtc &&
            a.Status != "CANCELLED" &&
            a.Status != "DECLINED")
        .Select(a => a.SlotTime)
        .ToListAsync();

    var bookedSet = booked.ToHashSet();

    var availableIso = slots
        .Where(s => !bookedSet.Contains(s))
        .OrderBy(s => s)
        .Select(s => s.ToString("O"))
        .ToList();

    return Results.Ok(availableIso);
});

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
    {
        return Results.NotFound(new { error = "Appointment not found" });
    }

    if (appointment.Status == "Paid")
    {
        return Results.Ok(new { message = "Appointment already marked as Paid", appointment });
    }

    if (appointment.Status == "Cancelled")
    {
        return Results.BadRequest(new { error = "Cannot update cancelled appointment" });
    }

    // Update appointment status to Paid after successful payment
    appointment.Status = "Paid";

    var paymentConfirmedEvent = new
    {
        appointment.Id,
        appointment.UserId,
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

app.MapGet("/metrics", () => Results.Json(Metrics.Snapshot()));

app.MapGet("/metrics/prometheus", () =>
{
    return Results.Text(
        Metrics.SnapshotPrometheus(),
        "text/plain");
});

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
    {
        return Results.NotFound();
    }

    if (string.Equals(appointment.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(appointment.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
        return Results.BadRequest("Appointment already cancelled");

    if (appointment.SlotTime < DateTime.UtcNow)
        return Results.BadRequest("Cannot cancel past appointment");

    appointment.Status = "CANCELLED";

    var cancelEvent = new
    {
        appointment.Id,
        appointment.PatientId,
        appointment.UserId,
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

    return Results.Ok();
}));

RequireApiKey(
app.MapPatch("/appointments/{id:guid}/decline",
async (
    Guid id,
    AppointmentDbContext db,
    ICorrelationIdAccessor correlationAccessor,
    ILogger<Program> logger) =>
{
    var appointment = await db.Appointments
        .FirstOrDefaultAsync(a => a.Id == id);

    if (appointment is null)
    {
        return Results.NotFound(new { error = "Appointment not found" });
    }

    if (string.Equals(appointment.Status, "DECLINED", StringComparison.OrdinalIgnoreCase))
    {
        return Results.Ok(new { message = "Appointment already declined", appointmentId = appointment.Id });
    }

    if (string.Equals(appointment.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new { error = "Cannot decline cancelled appointment" });
    }

    appointment.Status = "DECLINED";

    db.OutboxMessages.Add(
        CreateOutboxMessage(
            "appointment.declined",
            new
            {
                appointment.Id,
                appointment.UserId,
                appointment.PatientId,
                appointment.DoctorId,
                appointment.SlotTime,
                appointment.Status
            },
            correlationAccessor.CorrelationId));

    await db.SaveChangesAsync();

    logger.LogInformation(
        "Appointment declined {AppointmentId}",
        appointment.Id);

    return Results.Ok(new
    {
        appointmentId = appointment.Id,
        status = appointment.Status
    });
}));

RequireApiKey(
app.MapPost("/appointments",
async (
    HttpContext http,
    CreateAppointmentRequest request,
    AppointmentDbContext db,
    IConnectionMultiplexer redis,
    ICorrelationIdAccessor correlationAccessor,
    ILogger<Program> logger) =>
{
    if (request.DoctorId == Guid.Empty)
        return Results.BadRequest("DoctorId is required.");

    if (string.IsNullOrWhiteSpace(request.HospitalId))
        return Results.BadRequest("HospitalId is required.");

    var slotTimeUtc = ToUtc(request.SlotTime);
    if (slotTimeUtc < DateTime.UtcNow)
        return Results.BadRequest("Selected slot must be current or future time.");

    var appointmentDateUtc = DateTime.SpecifyKind(slotTimeUtc.Date, DateTimeKind.Utc);

    if (request.UserId is null && request.Guest is null)
        return Results.BadRequest("UserId or guest details are required.");

    if (request.UserId is null)
    {
        if (string.IsNullOrWhiteSpace(request.Guest!.FullName) ||
            string.IsNullOrWhiteSpace(request.Guest.Email) ||
            string.IsNullOrWhiteSpace(request.Guest.PhoneNumber) ||
            string.IsNullOrWhiteSpace(request.Guest.Area) ||
            string.IsNullOrWhiteSpace(request.Guest.NicOrPassport))
        {
            return Results.BadRequest("All guest fields are required.");
        }
    }

    const int maxRetries = 3;

    var idempotencyKey = http.Request.Headers["Idempotency-Key"]
        .FirstOrDefault();
    var redisLockUnavailable = false;

    if (string.IsNullOrWhiteSpace(idempotencyKey))
    {
        idempotencyKey = null;
    }

    var lockKey =
        $"lock:appointment:{request.DoctorId}:{slotTimeUtc:O}";
    var sequenceLockKey =
        $"lock:appointment-seq:{request.DoctorId}:{appointmentDateUtc:yyyy-MM-dd}";

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
                    return Results.Conflict("Slot already booked");
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

            await db.Database.ExecuteSqlRawAsync(
                "SELECT pg_advisory_xact_lock(hashtext({0}))",
                sequenceLockKey);

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
                            .Include(a => a.GuestUser)
                            .AsNoTracking()
                            .Where(a => a.Id == existingRecord.AppointmentId)
                            .Select(ToAppointmentDtoExpression())
                            .FirstOrDefaultAsync();

                    return Results.Ok(existingAppointment);
                }
            }

            var availability = await db.DoctorAvailabilities
                .AsNoTracking()
                .FirstOrDefaultAsync(a =>
                    a.DoctorId == request.DoctorId &&
                    a.HospitalId == request.HospitalId &&
                    a.SlotTime == slotTimeUtc);

            if (availability is null)
                return Results.BadRequest("Selected slot is not available.");

            var exists = await db.Appointments
                .AnyAsync(a =>
                    a.DoctorId == request.DoctorId &&
                    a.SlotTime == slotTimeUtc &&
                    a.Status != "CANCELLED" &&
                    a.Status != "DECLINED"
                );

            if (exists)
            {
                logger.LogWarning(
                    "Booking conflict detected for doctor {DoctorId} at {SlotTime}",
                    request.DoctorId,
                    slotTimeUtc
                );

                Metrics.IncAppointmentsConflict();
                return Results.Conflict("Slot already booked");
            }

            if (request.UserId.HasValue)
            {
                var hasExistingForUser = await db.Appointments
                    .AnyAsync(a =>
                        a.DoctorId == request.DoctorId &&
                        a.AppointmentDate == appointmentDateUtc &&
                        a.UserId == request.UserId.Value &&
                        a.Status != "CANCELLED" &&
                        a.Status != "DECLINED");

                if (hasExistingForUser)
                {
                    return Results.BadRequest("You already have an active appointment with this doctor for the selected date.");
                }
            }
            else
            {
                var guestEmail = request.Guest!.Email.Trim();
                var guestNicOrPassport = request.Guest.NicOrPassport.Trim();

                var hasExistingForGuest = await db.Appointments
                    .Include(a => a.GuestUser)
                    .AnyAsync(a =>
                        a.DoctorId == request.DoctorId &&
                        a.AppointmentDate == appointmentDateUtc &&
                        a.Status != "CANCELLED" &&
                        a.Status != "DECLINED" &&
                        a.GuestUser != null &&
                        EF.Functions.ILike(a.GuestUser.Email, guestEmail) &&
                        EF.Functions.ILike(a.GuestUser.NicOrPassport, guestNicOrPassport));

                if (hasExistingForGuest)
                {
                    return Results.BadRequest("You already have an active appointment with this doctor for the selected date.");
                }
            }

            GuestUser? guest = null;
            if (request.UserId is null)
            {
                guest = new GuestUser
                {
                    Id = Guid.NewGuid(),
                    FullName = request.Guest!.FullName.Trim(),
                    Email = request.Guest.Email.Trim(),
                    PhoneNumber = request.Guest.PhoneNumber.Trim(),
                    Area = request.Guest.Area.Trim(),
                    NicOrPassport = request.Guest.NicOrPassport.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                db.GuestUsers.Add(guest);
            }

            var pricing = ToPricingResponse(availability);

            var dailyCount = await db.Appointments
                .CountAsync(a =>
                    a.DoctorId == request.DoctorId &&
                    a.AppointmentDate == appointmentDateUtc &&
                    a.Status != "CANCELLED" &&
                    a.Status != "DECLINED");

            var appointmentNumber = dailyCount + 1;
            if (appointmentNumber > 20)
            {
                return Results.BadRequest("Doctor has reached maximum patient limit for selected date.");
            }

            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                PatientId = request.UserId ?? Guid.NewGuid(),
                UserId = request.UserId,
                DoctorId = request.DoctorId,
                DoctorName = string.IsNullOrWhiteSpace(request.DoctorName)
                    ? availability.DoctorName
                    : request.DoctorName,
                HospitalId = request.HospitalId,
                HospitalName = string.IsNullOrWhiteSpace(request.HospitalName)
                    ? availability.HospitalName
                    : request.HospitalName,
                Specialization = string.IsNullOrWhiteSpace(request.Specialization)
                    ? availability.Specialization
                    : request.Specialization,
                SlotTime = slotTimeUtc,
                AppointmentDate = appointmentDateUtc,
                AppointmentNumber = appointmentNumber,
                BookingReferenceId = BuildBookingReference(),
                DoctorFee = pricing.DoctorFee,
                HospitalFee = pricing.HospitalFee,
                EChannellingFee = pricing.EChannellingFee,
                Discount = pricing.Discount,
                TotalFee = pricing.TotalFee,
                GuestUserId = guest?.Id,
                Status = "PENDING_PAYMENT",
                CreatedAt = DateTime.UtcNow
            };

            db.Appointments.Add(appointment);

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

            var createEvent = new
            {
                appointment.Id,
                appointment.UserId,
                appointment.PatientId,
                appointment.DoctorId,
                appointment.AppointmentDate,
                appointment.AppointmentNumber,
                appointment.SlotTime,
                appointment.Status,
                appointment.BookingReferenceId
            };

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

            var response = await db.Appointments
                .Include(a => a.GuestUser)
                .AsNoTracking()
                .Where(a => a.Id == appointment.Id)
                .Select(ToAppointmentDtoExpression())
                .FirstAsync();

            return Results.Ok(response);
        }
        catch (Exception ex) when (IsUniqueConstraintViolation(ex))
        {
            if (tx is not null)
            {
                await TryRollbackAsync(tx);
            }

            logger.LogWarning(
                "Booking conflict detected for doctor {DoctorId} at {SlotTime}",
                request.DoctorId,
                slotTimeUtc
            );

            Metrics.IncAppointmentsConflict();
            return Results.Conflict("Slot already booked");
        }
        catch (Exception ex) when (IsSerializationFailure(ex))
        {
            if (tx is not null)
            {
                await TryRollbackAsync(tx);
            }

            if (attempt == maxRetries)
            {
                logger.LogWarning(
                    "Booking conflict after retries for doctor {DoctorId} at {SlotTime}",
                    request.DoctorId,
                    slotTimeUtc
                );

                Metrics.IncAppointmentsConflict();
                return Results.Conflict("Booking conflict, please retry");
            }

            db.ChangeTracker.Clear();

            var delayMs = (int)(100 * Math.Pow(2, attempt - 1));
            await Task.Delay(delayMs);
        }
        finally
        {
            if (tx is not null)
                await tx.DisposeAsync();

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

RequireApiKey(
app.MapPost("/appointments/{id:guid}/payment-success",
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

    if (appointment.Status == "CONFIRMED")
        return Results.Ok(new
        {
            appointmentId = appointment.Id,
            status = appointment.Status
        });

    appointment.Status = "CONFIRMED";

    db.OutboxMessages.Add(
        CreateOutboxMessage(
            "appointment.confirmed",
            new
            {
                appointment.Id,
                appointment.UserId,
                appointment.PatientId,
                appointment.DoctorId,
                appointment.SlotTime,
                appointment.Status
            },
            correlationAccessor.CorrelationId));

    await db.SaveChangesAsync();

    logger.LogInformation(
        "Appointment confirmed {AppointmentId}",
        appointment.Id);

    return Results.Ok(new
    {
        appointmentId = appointment.Id,
        status = appointment.Status
    });
}));

RequireApiKey(
app.MapPost("/payments/initiate",
async (
    PaymentInitiateRequest request,
    AppointmentDbContext db) =>
{
    var appointment = await db.Appointments
        .AsNoTracking()
        .FirstOrDefaultAsync(a => a.Id == request.AppointmentId);

    if (appointment is null)
        return Results.NotFound("Appointment not found.");

    return Results.Ok(new
    {
        appointmentId = appointment.Id,
        status = "INITIATED",
        redirectUrl = $"/payment?appointmentId={appointment.Id}",
        simulated = true
    });
}));

RequireApiKey(
app.MapPost("/payments/callback",
async (
    PaymentCallbackRequest request,
    AppointmentDbContext db,
    ICorrelationIdAccessor correlationAccessor) =>
{
    var appointment = await db.Appointments
        .FirstOrDefaultAsync(a => a.Id == request.AppointmentId);

    if (appointment is null)
        return Results.NotFound("Appointment not found.");

    if (!string.Equals(request.PaymentStatus, "SUCCESS", StringComparison.OrdinalIgnoreCase))
    {
        return Results.Ok(new
        {
            appointmentId = appointment.Id,
            status = appointment.Status,
            paymentStatus = request.PaymentStatus
        });
    }

    if (appointment.Status != "CONFIRMED")
    {
        appointment.Status = "CONFIRMED";

        db.OutboxMessages.Add(
            CreateOutboxMessage(
                "appointment.confirmed",
                new
                {
                    appointment.Id,
                    appointment.UserId,
                    appointment.PatientId,
                    appointment.DoctorId,
                    appointment.SlotTime,
                    appointment.Status
                },
                correlationAccessor.CorrelationId));

        await db.SaveChangesAsync();
    }

    return Results.Ok(new
    {
        appointmentId = appointment.Id,
        status = appointment.Status,
        paymentStatus = "SUCCESS"
    });
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
        {
            return Results.Unauthorized();
        }

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
    {
        return true;
    }

    if (ex is DbUpdateException dbEx &&
        dbEx.InnerException is PostgresException pgInner &&
        pgInner.SqlState == sqlState)
    {
        return true;
    }

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

static DateTime ToUtc(DateTime dateTime)
{
    return dateTime.Kind switch
    {
        DateTimeKind.Utc => dateTime,
        DateTimeKind.Local => dateTime.ToUniversalTime(),
        _ => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
    };
}

static string BuildBookingReference()
{
    return $"BK-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..22];
}

static async Task<List<ApprovedDoctorDto>> FetchApprovedDoctorsAsync(IConfiguration config)
{
    var configuredBaseUrl =
        config["DoctorService:BaseUrl"] ??
        config["DoctorService__BaseUrl"];

    var candidateBaseUrls = new List<string>();

    if (!string.IsNullOrWhiteSpace(configuredBaseUrl))
        candidateBaseUrls.Add(configuredBaseUrl);

    candidateBaseUrls.Add("http://doctor-service:8080");
    candidateBaseUrls.Add("http://host.docker.internal:30002");

    using var client = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(3)
    };

    foreach (var baseUrl in candidateBaseUrls.Distinct(StringComparer.OrdinalIgnoreCase))
    {
        try
        {
            var endpoint = $"{baseUrl.TrimEnd('/')}/doctors/approved";

            var doctors = await client.GetFromJsonAsync<List<ApprovedDoctorDto>>(endpoint);
            if (doctors is not null)
                return doctors;
        }
        catch
        {
        }
    }

    return new List<ApprovedDoctorDto>();
}

static string NormalizeHospitalId(string hospital)
{
    return hospital
        .Trim()
        .ToUpperInvariant()
        .Replace(" ", "-");
}

static PricingResponse ToPricingResponse(DoctorAvailability slot)
{
    var total = slot.DoctorFee +
                slot.HospitalFee +
                slot.EChannellingFee -
                slot.Discount;

    return new PricingResponse
    {
        DoctorFee = slot.DoctorFee,
        HospitalFee = slot.HospitalFee,
        EChannellingFee = slot.EChannellingFee,
        Discount = slot.Discount,
        TotalFee = Math.Max(total, 0)
    };
}

static System.Linq.Expressions.Expression<Func<Appointment, AppointmentDto>> ToAppointmentDtoExpression()
{
    return a => new AppointmentDto
    {
        Id = a.Id,
        PatientId = a.PatientId,
        UserId = a.UserId,
        DoctorId = a.DoctorId,
        DoctorName = a.DoctorName,
        HospitalId = a.HospitalId,
        HospitalName = a.HospitalName,
        Specialization = a.Specialization,
        SlotTime = a.SlotTime,
        AppointmentDate = a.AppointmentDate,
        AppointmentNumber = a.AppointmentNumber,
        BookingReferenceId = a.BookingReferenceId,
        DoctorFee = a.DoctorFee,
        HospitalFee = a.HospitalFee,
        EChannellingFee = a.EChannellingFee,
        Discount = a.Discount,
        TotalFee = a.TotalFee,
        GuestUserId = a.GuestUserId,
        GuestUser = a.GuestUser == null
            ? null
            : new GuestUserDto
            {
                Id = a.GuestUser.Id,
                FullName = a.GuestUser.FullName,
                Email = a.GuestUser.Email,
                PhoneNumber = a.GuestUser.PhoneNumber,
                Area = a.GuestUser.Area,
                NicOrPassport = a.GuestUser.NicOrPassport
            },
        Status = a.Status,
        CreatedAt = a.CreatedAt
    };
}

static async Task EnsureDynamicSchemaAsync(AppointmentDbContext db)
{
    await db.Database.ExecuteSqlRawAsync(
        """
        CREATE TABLE IF NOT EXISTS "DoctorAvailabilities" (
            "Id" uuid PRIMARY KEY,
            "DoctorId" uuid NOT NULL,
            "DoctorName" text NOT NULL,
            "Specialization" text NOT NULL,
            "HospitalId" text NOT NULL,
            "HospitalName" text NOT NULL,
            "SlotTime" timestamp with time zone NOT NULL,
            "DoctorFee" numeric(10,2) NOT NULL,
            "HospitalFee" numeric(10,2) NOT NULL,
            "EChannellingFee" numeric(10,2) NOT NULL,
            "Discount" numeric(10,2) NOT NULL
        );

        CREATE UNIQUE INDEX IF NOT EXISTS "IX_DoctorAvailabilities_DoctorId_SlotTime"
        ON "DoctorAvailabilities" ("DoctorId", "SlotTime");

        ALTER TABLE "DoctorAvailabilities" ADD COLUMN IF NOT EXISTS "DoctorName" text NOT NULL DEFAULT '';
        ALTER TABLE "DoctorAvailabilities" ADD COLUMN IF NOT EXISTS "Specialization" text NOT NULL DEFAULT '';
        ALTER TABLE "DoctorAvailabilities" ADD COLUMN IF NOT EXISTS "HospitalId" text NOT NULL DEFAULT '';
        ALTER TABLE "DoctorAvailabilities" ADD COLUMN IF NOT EXISTS "HospitalName" text NOT NULL DEFAULT '';
        ALTER TABLE "DoctorAvailabilities" ADD COLUMN IF NOT EXISTS "IsAvailable" boolean NOT NULL DEFAULT TRUE;
        ALTER TABLE "DoctorAvailabilities" ADD COLUMN IF NOT EXISTS "DoctorFee" numeric(10,2) NOT NULL DEFAULT 0;
        ALTER TABLE "DoctorAvailabilities" ADD COLUMN IF NOT EXISTS "HospitalFee" numeric(10,2) NOT NULL DEFAULT 0;
        ALTER TABLE "DoctorAvailabilities" ADD COLUMN IF NOT EXISTS "EChannellingFee" numeric(10,2) NOT NULL DEFAULT 0;
        ALTER TABLE "DoctorAvailabilities" ADD COLUMN IF NOT EXISTS "Discount" numeric(10,2) NOT NULL DEFAULT 0;
        ALTER TABLE "DoctorAvailabilities" ALTER COLUMN "IsAvailable" SET DEFAULT TRUE;

        CREATE TABLE IF NOT EXISTS "GuestUsers" (
            "Id" uuid PRIMARY KEY,
            "FullName" text NOT NULL,
            "Email" text NOT NULL,
            "PhoneNumber" text NOT NULL,
            "Area" text NOT NULL,
            "NicOrPassport" text NOT NULL,
            "CreatedAt" timestamp with time zone NOT NULL
        );

        ALTER TABLE "Appointments" ADD COLUMN IF NOT EXISTS "UserId" uuid NULL;
        ALTER TABLE "Appointments" ADD COLUMN IF NOT EXISTS "DoctorName" text NOT NULL DEFAULT '';
        ALTER TABLE "Appointments" ADD COLUMN IF NOT EXISTS "HospitalId" text NOT NULL DEFAULT '';
        ALTER TABLE "Appointments" ADD COLUMN IF NOT EXISTS "HospitalName" text NOT NULL DEFAULT '';
        ALTER TABLE "Appointments" ADD COLUMN IF NOT EXISTS "Specialization" text NOT NULL DEFAULT '';
        ALTER TABLE "Appointments" ADD COLUMN IF NOT EXISTS "AppointmentDate" timestamp with time zone;
        ALTER TABLE "Appointments" ADD COLUMN IF NOT EXISTS "AppointmentNumber" integer;
        ALTER TABLE "Appointments" ADD COLUMN IF NOT EXISTS "BookingReferenceId" text NOT NULL DEFAULT '';
        ALTER TABLE "Appointments" ADD COLUMN IF NOT EXISTS "DoctorFee" numeric(10,2) NOT NULL DEFAULT 0;
        ALTER TABLE "Appointments" ADD COLUMN IF NOT EXISTS "HospitalFee" numeric(10,2) NOT NULL DEFAULT 0;
        ALTER TABLE "Appointments" ADD COLUMN IF NOT EXISTS "EChannellingFee" numeric(10,2) NOT NULL DEFAULT 0;
        ALTER TABLE "Appointments" ADD COLUMN IF NOT EXISTS "Discount" numeric(10,2) NOT NULL DEFAULT 0;
        ALTER TABLE "Appointments" ADD COLUMN IF NOT EXISTS "TotalFee" numeric(10,2) NOT NULL DEFAULT 0;
        ALTER TABLE "Appointments" ADD COLUMN IF NOT EXISTS "GuestUserId" uuid NULL;

        UPDATE "Appointments"
        SET "AppointmentDate" = date_trunc('day', "SlotTime")
        WHERE "AppointmentDate" IS NULL;

        WITH numbered AS (
            SELECT
                "Id",
                row_number() OVER (
                    PARTITION BY "DoctorId", "AppointmentDate"
                    ORDER BY "CreatedAt", "Id"
                ) AS seq
            FROM "Appointments"
            WHERE "AppointmentNumber" IS NULL OR "AppointmentNumber" <= 0
        )
        UPDATE "Appointments" a
        SET "AppointmentNumber" = numbered.seq
        FROM numbered
        WHERE a."Id" = numbered."Id";

        ALTER TABLE "Appointments" ALTER COLUMN "AppointmentDate" SET NOT NULL;
        ALTER TABLE "Appointments" ALTER COLUMN "AppointmentNumber" SET NOT NULL;

        CREATE INDEX IF NOT EXISTS "IX_Appointments_Status" ON "Appointments" ("Status");
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_Appointments_DoctorId_AppointmentDate_AppointmentNumber"
        ON "Appointments" ("DoctorId", "AppointmentDate", "AppointmentNumber");

        CREATE UNIQUE INDEX IF NOT EXISTS "IX_Appointments_DoctorId_AppointmentDate_UserId_Active"
        ON "Appointments" ("DoctorId", "AppointmentDate", "UserId")
        WHERE "UserId" IS NOT NULL AND "Status" <> 'CANCELLED' AND "Status" <> 'DECLINED';

        DO $$
        BEGIN
            IF NOT EXISTS (
                SELECT 1
                FROM pg_constraint
                WHERE conname = 'FK_Appointments_GuestUsers_GuestUserId'
            ) THEN
                ALTER TABLE "Appointments"
                ADD CONSTRAINT "FK_Appointments_GuestUsers_GuestUserId"
                FOREIGN KEY ("GuestUserId")
                REFERENCES "GuestUsers" ("Id")
                ON DELETE SET NULL;
            END IF;
        END $$;
        """);
}

static async Task SeedDoctorAvailabilitiesAsync(AppointmentDbContext db)
{
    if (await db.DoctorAvailabilities.AnyAsync())
        return;

    var path = Path.Combine(
        AppContext.BaseDirectory,
        "MockData",
        "doctor-availability.json");

    if (!File.Exists(path))
        return;

    var json = await File.ReadAllTextAsync(path);

    var rows = JsonSerializer.Deserialize<List<DoctorAvailabilityMockRow>>(
        json,
        new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

    if (rows is null || rows.Count == 0)
        return;

    foreach (var row in rows)
    {
        if (!DateOnly.TryParseExact(
                row.Date,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date))
            continue;

        if (!TimeOnly.TryParseExact(
                row.Time,
                "HH:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var time))
            continue;

        var slotTime = ToUtc(date.ToDateTime(time));

        db.DoctorAvailabilities.Add(new DoctorAvailability
        {
            Id = Guid.NewGuid(),
            DoctorId = row.DoctorId,
            DoctorName = row.DoctorName,
            Specialization = row.Specialization,
            HospitalId = row.HospitalId,
            HospitalName = row.HospitalName,
            SlotTime = slotTime,
            DoctorFee = row.DoctorFee,
            HospitalFee = row.HospitalFee,
            EChannellingFee = row.EChannellingFee,
            Discount = row.Discount
        });
    }

    await db.SaveChangesAsync();
}

internal class DoctorAvailabilityMockRow
{
    public Guid DoctorId { get; set; }

    public string DoctorName { get; set; } = string.Empty;

    public string Specialization { get; set; } = string.Empty;

    public string HospitalId { get; set; } = string.Empty;

    public string HospitalName { get; set; } = string.Empty;

    public string Date { get; set; } = string.Empty;

    public string Time { get; set; } = string.Empty;

    public decimal DoctorFee { get; set; }

    public decimal HospitalFee { get; set; }

    public decimal EChannellingFee { get; set; }

    public decimal Discount { get; set; }
}

internal class ApprovedDoctorDto
{
    public string FullName { get; set; } = string.Empty;

    public string Specialization { get; set; } = string.Empty;

    public string Hospital { get; set; } = string.Empty;
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

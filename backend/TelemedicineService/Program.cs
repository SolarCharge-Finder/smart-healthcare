using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using Serilog;
using Serilog.Context;
using Serilog.Formatting.Json;

using TelemedicineService.Data;
using TelemedicineService.Models;
using TelemedicineService.Services;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("TelemedicineService.Tests")]

var builder = WebApplication.CreateBuilder(args);

var serviceName = builder.Configuration["ServiceName"] ?? "telemedicine-service";
var environment = builder.Environment.EnvironmentName;

// Logging
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("serviceName", serviceName)
    .Enrich.WithProperty("environment", environment)
    .WriteTo.Console(new JsonFormatter())
    .CreateLogger();

builder.Host.UseSerilog();

// Swagger
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

builder.Services
    .AddOptions<TelemedicineSessionOptions>()
    .Bind(builder.Configuration.GetSection(TelemedicineSessionOptions.SectionName));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var configuredKey =
            Environment.GetEnvironmentVariable("JWT_KEY") ??
            builder.Configuration["Jwt_Key"] ??
            builder.Configuration["Jwt:Key"];

        if (string.IsNullOrWhiteSpace(configuredKey))
        {
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
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(configuredKey))
        };
    });

builder.Services.AddAuthorization();

// CORS
var frontendOrigin = builder.Configuration["Frontend:Origin"];
var enableCors =
    builder.Environment.IsDevelopment() ||
    builder.Environment.IsEnvironment("Testing") ||
    !string.IsNullOrWhiteSpace(frontendOrigin);

if (enableCors)
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("FrontendPolicy", policy =>
        {
            var allowedOrigins = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "http://localhost:3000",
                "http://127.0.0.1:3000",
                "http://localhost:3001",
                "http://127.0.0.1:3001",
                "http://localhost:3002",
                "http://127.0.0.1:3002"
            };

            if (!string.IsNullOrWhiteSpace(frontendOrigin))
            {
                allowedOrigins.Add(frontendOrigin);
            }

            policy.WithOrigins(allowedOrigins.ToArray())
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });
}

// Database
builder.Services.AddDbContext<TelemedicineDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

// Agora configuration
builder.Services
    .AddOptions<AgoraOptions>()
    .Bind(builder.Configuration.GetSection(AgoraOptions.SectionName));

// Services
builder.Services.AddScoped<IAgoraTokenService, AgoraTokenService>();
builder.Services.AddScoped<ITelemedicineService, TelemedicineSessionService>();

// HTTP Client for AppointmentService
builder.Services.AddHttpClient<ITelemedicineService, TelemedicineSessionService>(
    (sp, client) =>
    {
        var appointmentServiceUrl = sp.GetRequiredService<IConfiguration>()
            ["AppointmentService:BaseUrl"] ?? "http://localhost:8080/";

        client.BaseAddress = new Uri(appointmentServiceUrl);
    });

// Build app
var app = builder.Build();

// Ensure database created and run migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TelemedicineDbContext>();

    if (app.Environment.IsEnvironment("Testing"))
    {
        db.Database.EnsureCreated();
    }
    else
    {
        db.Database.Migrate();
    }
}

// Middleware
app.UseSwagger();
app.UseSwaggerUI();

if (enableCors)
{
    app.UseCors("FrontendPolicy");
    // app.UseCors("frontend");
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    const string headerName = "X-Correlation-ID";
    var correlationId = context.Request.Headers[headerName].FirstOrDefault();

    if (string.IsNullOrWhiteSpace(correlationId))
    {
        correlationId = Guid.NewGuid().ToString();
    }

    context.Response.Headers[headerName] = correlationId;
    context.Items["CorrelationId"] = correlationId;

    await next();
});

// Endpoints
var telemedicineGroup = app.MapGroup("/telemedicine")
    .WithTags("Telemedicine")
    .RequireAuthorization();

RequireApiKey(telemedicineGroup.MapPost("/session", CreateTelemedicineSession)
    .WithName("CreateTelemedicineSession")
    .WithDescription("Create a new telemedicine session for an appointment. Generates Agora tokens server-side.")
    .Produces<TelemedicineSessionResponse>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound)
    .Produces(StatusCodes.Status400BadRequest));

RequireApiKey(telemedicineGroup.MapGet("/session/{appointmentId:guid}", GetTelemedicineSession)
    .WithName("GetTelemedicineSession")
    .WithDescription("Get an existing active telemedicine session for an appointment.")
    .Produces<TelemedicineSessionResponse>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound));

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = serviceName }))
    .WithName("HealthCheck")
    .Produces(StatusCodes.Status200OK);

// Endpoint handlers
async Task<IResult> CreateTelemedicineSession(
    CreateSessionRequest request,
    ClaimsPrincipal user,
    ITelemedicineService telemedicineService,
    ILogger<Program> logger,
    CancellationToken cancellationToken)
{
    try
    {
        var requesterUserId = GetRequesterUserId(user);
        if (requesterUserId is null)
        {
            return Results.Unauthorized();
        }

        if (request.AppointmentId == Guid.Empty)
        {
            logger.LogWarning("CreateTelemedicineSession called with empty appointmentId");
            return Results.BadRequest(new { error = "AppointmentId cannot be empty" });
        }

        var response = await telemedicineService.CreateSessionAsync(
            request.AppointmentId,
            requesterUserId.Value,
            cancellationToken);

        return Results.Ok(response);
    }
    catch (KeyNotFoundException ex)
    {
        logger.LogWarning(ex, "Appointment not found");
        return Results.NotFound(new { error = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        logger.LogWarning(ex, "Invalid appointment state");
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unexpected error in CreateTelemedicineSession");
        return Results.StatusCode(500);
    }
}

async Task<IResult> GetTelemedicineSession(
    Guid appointmentId,
    ClaimsPrincipal user,
    ITelemedicineService telemedicineService,
    ILogger<Program> logger,
    CancellationToken cancellationToken)
{
    try
    {
        var requesterUserId = GetRequesterUserId(user);
        if (requesterUserId is null)
        {
            return Results.Unauthorized();
        }

        if (appointmentId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "AppointmentId cannot be empty" });
        }

        var session = await telemedicineService.GetActiveSessionAsync(appointmentId, cancellationToken);

        if (session is null)
        {
            return Results.NotFound(new { error = $"No active session found for appointment {appointmentId}" });
        }

        // Reuse CreateSessionAsync to get fresh tokens for the existing session
        var response = await telemedicineService.CreateSessionAsync(
            appointmentId,
            requesterUserId.Value,
            cancellationToken);
        return Results.Ok(response);
    }
    catch (KeyNotFoundException ex)
    {
        logger.LogWarning(ex, "Appointment not found for session lookup");
        return Results.NotFound(new { error = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        logger.LogWarning(ex, "Invalid appointment state for session lookup");
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unexpected error in GetTelemedicineSession");
        return Results.StatusCode(500);
    }
}

static Guid? GetRequesterUserId(ClaimsPrincipal user)
{
    var rawUserId =
        user.FindFirstValue(ClaimTypes.NameIdentifier) ??
        user.FindFirstValue("sub");

    return Guid.TryParse(rawUserId, out var parsedUserId)
        ? parsedUserId
        : null;
}

static RouteHandlerBuilder RequireApiKey(RouteHandlerBuilder endpoint)
{
    return endpoint.AddEndpointFilter(async (context, next) =>
    {
        var http = context.HttpContext;

        var configured =
            Environment.GetEnvironmentVariable("API_KEY") ??
            http.RequestServices.GetRequiredService<IConfiguration>()["ApiKey"] ??
            "dev-key";

        var provided = http.Request.Headers["X-API-KEY"]
            .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(configured) &&
            !string.Equals(configured, provided, StringComparison.Ordinal))
        {
            return Results.Unauthorized();
        }

        return await next(context);
    });
}

app.Run();

// Make Program class accessible for integration tests using WebApplicationFactory
internal partial class Program { }

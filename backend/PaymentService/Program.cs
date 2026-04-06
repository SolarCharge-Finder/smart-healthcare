using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PaymentService.Data;
using PaymentService.Models;
using PaymentService.Services;
using Serilog;
using Serilog.Context;
using Serilog.Formatting.Json;
using Stripe;
using System.Text.Json;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("PaymentService.Tests")]

var builder = WebApplication.CreateBuilder(args);

var serviceName =
    builder.Configuration["ServiceName"] ??
    "payment-service";

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

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:3001", "http://localhost:3002")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services
    .AddOptions<StripeOptions>()
    .Bind(builder.Configuration.GetSection(StripeOptions.SectionName));

builder.Services
    .AddOptions<AppointmentServiceOptions>()
    .Bind(builder.Configuration.GetSection(AppointmentServiceOptions.SectionName));

builder.Services
    .AddOptions<PaymentPricingOptions>()
    .Bind(builder.Configuration.GetSection(PaymentPricingOptions.SectionName));

builder.Services.AddHttpClient<IAppointmentPricingService, AppointmentPricingService>(
    (sp, client) =>
    {
        var options = sp.GetRequiredService<IOptions<AppointmentServiceOptions>>().Value;
        client.BaseAddress = new Uri(options.BaseUrl);
    });

builder.Services.AddSingleton<PaymentIntentService>();

builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

builder.Services.AddScoped<IPaymentService, PaymentProcessorService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<PaymentDbContext>();

    db.Database.EnsureCreated();

    await db.Database.ExecuteSqlRawAsync(
        """
        CREATE TABLE IF NOT EXISTS "Payments" (
            "Id" uuid PRIMARY KEY,
            "AppointmentId" uuid NOT NULL,
            "StripePaymentIntentId" text NOT NULL,
            "ClientSecret" text NOT NULL,
            "Amount" bigint NOT NULL,
            "Currency" character varying(10) NOT NULL,
            "Status" character varying(30) NOT NULL,
            "FailureReason" character varying(250),
            "CreatedAt" timestamp with time zone NOT NULL,
            "UpdatedAt" timestamp with time zone NOT NULL
        );

        CREATE UNIQUE INDEX IF NOT EXISTS "IX_Payments_AppointmentId"
        ON "Payments" ("AppointmentId");
        """);
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowFrontend");

app.UseHttpsRedirection();

var stripeOptions = app.Services
    .GetRequiredService<IOptions<StripeOptions>>()
    .Value;

if (string.IsNullOrWhiteSpace(stripeOptions.SecretKey))
{
    Log.Warning("Stripe secret key is not configured. Payment intent creation will fail until configured.");
}
else
{
    StripeConfiguration.ApiKey = stripeOptions.SecretKey;
}

app.Use(async (context, next) =>
{
    const string headerName = "X-Correlation-ID";

    var correlationId =
        context.Request.Headers[headerName]
            .FirstOrDefault();

    if (string.IsNullOrWhiteSpace(correlationId))
        correlationId = Guid.NewGuid().ToString();

    context.Response.Headers[headerName] = correlationId;

    using (LogContext.PushProperty(
        "correlationId", correlationId))
    {
        await next();
    }
});

// Helper function to determine HTTP status code based on error message
static int DetermineStatusCode(string errorMessage)
{
    if (string.IsNullOrWhiteSpace(errorMessage))
        return 500;

    var lowerMessage = errorMessage.ToLowerInvariant();

    // Service unavailability errors
    if (lowerMessage.Contains("unavailable") ||
        lowerMessage.Contains("unreachable") ||
        lowerMessage.Contains("timed out") ||
        lowerMessage.Contains("cannot connect") ||
        lowerMessage.Contains("not configured"))
    {
        return 503;
    }

    // Default to 500 for other invalid operation exceptions
    return 500;
}

app.MapGet("/health/live", () =>
{
    return Results.Ok("alive");
});

app.MapGet("/health/ready",
async (PaymentDbContext db) =>
{
    var canConnect = await db.Database.CanConnectAsync();

    if (!canConnect)
        return Results.StatusCode(503);

    return Results.Ok("ready");
});

app.MapPost("/payments/intents",
async (HttpRequest httpRequest, IPaymentService paymentService, ILogger<Program> logger) =>
{
    CreatePaymentIntentRequest? request;

    try
    {
        using var jsonDoc = await JsonDocument.ParseAsync(httpRequest.Body);

        if (jsonDoc.RootElement.ValueKind != JsonValueKind.Object)
            return Results.BadRequest("Request body must be a JSON object.");

        var allowedFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "appointmentId"
        };

        foreach (var property in jsonDoc.RootElement.EnumerateObject())
        {
            if (!allowedFields.Contains(property.Name))
                return Results.BadRequest($"Unsupported field '{property.Name}'. Send only appointmentId.");
        }

        request = jsonDoc.RootElement.Deserialize<CreatePaymentIntentRequest>(
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (request is null)
            return Results.BadRequest("Invalid request body.");
    }
    catch (JsonException ex)
    {
        return Results.BadRequest($"Invalid JSON payload: {ex.Message}");
    }

    if (request.AppointmentId == Guid.Empty)
        return Results.BadRequest("AppointmentId is required");

    try
    {
        var payment = await paymentService.CreatePaymentIntentAsync(request);

        return Results.Ok(new CreatePaymentIntentResponse
        {
            PaymentId = payment.Id,
            PaymentIntentId = payment.StripePaymentIntentId,
            ClientSecret = payment.ClientSecret,
            Amount = payment.Amount,
            Currency = payment.Currency,
            Status = payment.Status
        });
    }
    catch (StripeException ex)
    {
        return Results.BadRequest($"Stripe error: {ex.Message}");
    }
    catch (KeyNotFoundException ex)
    {
        logger.LogInformation(ex, "Appointment not found for payment creation.");
        return Results.NotFound(new { detail = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        logger.LogWarning(ex, "Payment intent creation failed due to invalid service state.");

        var statusCode = DetermineStatusCode(ex.Message);

        return Results.Problem(
            title: "Payment initialization failed",
            detail: ex.Message,
            statusCode: statusCode);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unexpected error while creating payment intent.");
        return Results.Problem(
            title: "Payment initialization failed",
            detail: "Unexpected server error while creating payment intent.",
            statusCode: 500);
    }
});

app.MapPost("/payments/webhook",
async (HttpRequest httpRequest, IPaymentService paymentService, IOptions<StripeOptions> options, IOptions<AppointmentServiceOptions> appointmentOptions, ILogger<Program> logger, IHttpClientFactory httpClientFactory) =>
{
    var json = await new StreamReader(httpRequest.Body).ReadToEndAsync();
    var signature = httpRequest.Headers["Stripe-Signature"].ToString();
    var webhookSecret = options.Value.WebhookSecret;

    if (string.IsNullOrWhiteSpace(webhookSecret))
        return Results.Problem("Stripe webhook secret is not configured.", statusCode: 500);

    Event stripeEvent;

    try
    {
        stripeEvent = EventUtility.ConstructEvent(
            json,
            signature,
            webhookSecret,
            throwOnApiVersionMismatch: false);

        logger.LogInformation(
            "Stripe webhook event received: {EventType} ({EventId})",
            stripeEvent.Type,
            stripeEvent.Id);
    }
    catch (StripeException ex)
    {
        logger.LogWarning(ex, "Invalid Stripe webhook signature.");
        return Results.BadRequest($"Invalid Stripe webhook signature: {ex.Message}");
    }

    if (!stripeEvent.Type.StartsWith("payment_intent.", StringComparison.Ordinal))
    {
        logger.LogInformation(
            "Ignoring non-payment_intent webhook event: {EventType} ({EventId})",
            stripeEvent.Type,
            stripeEvent.Id);

        return Results.Ok(new { received = true, ignored = true, eventType = stripeEvent.Type });
    }

    if (stripeEvent.Data.Object is PaymentIntent intent)
    {
        var updatedPayment = await paymentService.UpdatePaymentFromStripeIntentAsync(intent);

        if (updatedPayment is null)
        {
            logger.LogWarning(
                "Webhook event {EventId} ignored because payment record was not found for StripePaymentIntentId {StripePaymentIntentId}.",
                stripeEvent.Id,
                intent.Id);
        }
        else
        {
            logger.LogInformation(
                "Webhook event {EventId} applied. Payment {PaymentId} is now {Status}.",
                stripeEvent.Id,
                updatedPayment.Id,
                updatedPayment.Status);

            // If payment is successful, notify AppointmentService to mark appointment as Paid
            if (updatedPayment.Status.Equals("Succeeded", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var appointmentServiceUrl = appointmentOptions.Value.BaseUrl;
                    var appointmentId = updatedPayment.AppointmentId;
                    
                    using var client = httpClientFactory.CreateClient();
                    var patchUrl = $"{appointmentServiceUrl}appointments/{appointmentId}/confirm-payment";
                    
                    var request = new HttpRequestMessage(HttpMethod.Patch, patchUrl);
                    request.Headers.Add("X-API-KEY", builder.Configuration["API_KEY"] ?? "");
                    
                    logger.LogInformation(
                        "Notifying AppointmentService to confirm payment for appointment {AppointmentId}",
                        appointmentId);
                    
                    var response = await client.SendAsync(request);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        logger.LogInformation(
                            "AppointmentService successfully confirmed payment for appointment {AppointmentId}",
                            appointmentId);
                    }
                    else
                    {
                        logger.LogWarning(
                            "AppointmentService returned {StatusCode} when confirming payment for appointment {AppointmentId}",
                            response.StatusCode,
                            appointmentId);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Failed to notify AppointmentService about payment confirmation for appointment {AppointmentId}",
                        updatedPayment.AppointmentId);
                    // Don't fail the webhook if notification fails - it can be retried
                }
            }
        }
    }
    else
    {
        logger.LogInformation(
            "Ignoring payment_intent webhook event with unsupported payload type: {EventType} ({EventId})",
            stripeEvent.Type,
            stripeEvent.Id);
    }

    return Results.Ok(new { received = true, eventType = stripeEvent.Type });
});

app.MapGet("/payments/config",
(IOptions<StripeOptions> options) =>
{
    return Results.Ok(new
    {
        publishableKey = options.Value.PublishableKey
    });
});

app.MapPost("/payments/{id:guid}/confirm",
async (Guid id, ConfirmPaymentRequest request, IPaymentService paymentService) =>
{
    if (!request.IsSuccess && string.IsNullOrWhiteSpace(request.FailureReason))
        return Results.BadRequest("FailureReason is required when IsSuccess is false");

    var payment = await paymentService.ConfirmPaymentAsync(id, request);

    if (payment is null)
        return Results.NotFound();

    return Results.Ok(payment);
});

app.MapGet("/payments/{id:guid}",
async (Guid id, IPaymentService paymentService) =>
{
    var payment = await paymentService.GetPaymentAsync(id);

    if (payment is null)
        return Results.NotFound();

    return Results.Ok(ToPaymentReadResponse(payment));
});

app.MapGet("/payments/appointment/{appointmentId:guid}",
async (Guid appointmentId, IPaymentService paymentService) =>
{
    var payment = await paymentService.GetPaymentByAppointmentAsync(appointmentId);

    if (payment is null)
        return Results.NotFound();

    return Results.Ok(ToPaymentReadResponse(payment));
});

app.MapGet("/payments",
async (IPaymentService paymentService) =>
{
    var payments = await paymentService.GetAllPaymentsAsync();
    return Results.Ok(payments.Select(ToPaymentReadResponse));
});

static object ToPaymentReadResponse(Payment payment)
{
    return new
    {
        payment.Id,
        payment.AppointmentId,
        payment.StripePaymentIntentId,
        payment.Amount,
        payment.Currency,
        payment.Status,
        payment.FailureReason,
        payment.CreatedAt,
        payment.UpdatedAt
    };
}

app.Run();

// Make Program class accessible for integration tests using WebApplicationFactory
internal partial class Program { }

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PaymentService.Data;
using PaymentService.Models;
using PaymentService.Services;
using Serilog;
using Serilog.Context;
using Serilog.Formatting.Json;
using Stripe;

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
		policy.WithOrigins("http://localhost:3000", "http://localhost:3001")
			.AllowAnyMethod()
			.AllowAnyHeader();
	});
});

builder.Services
	.AddOptions<StripeOptions>()
	.Bind(builder.Configuration.GetSection(StripeOptions.SectionName));

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
async (CreatePaymentIntentRequest request, IPaymentService paymentService) =>
{
	if (request.AppointmentId == Guid.Empty)
		return Results.BadRequest("AppointmentId is required");

	if (request.Amount <= 0)
		return Results.BadRequest("Amount must be greater than zero");

	if (string.IsNullOrWhiteSpace(request.Currency))
		return Results.BadRequest("Currency is required");

	try
	{
		var payment = await paymentService.CreatePaymentIntentAsync(request);

		return Results.Ok(new CreatePaymentIntentResponse
		{
			PaymentIntentId = payment.StripePaymentIntentId,
			ClientSecret = payment.ClientSecret,
			Status = payment.Status
		});
	}
	catch (StripeException ex)
	{
		return Results.BadRequest($"Stripe error: {ex.Message}");
	}
	catch (InvalidOperationException ex)
	{
		return Results.Problem(ex.Message, statusCode: 500);
	}
});


app.MapPost("/payments/webhook",
async (HttpRequest httpRequest, IPaymentService paymentService, IOptions<StripeOptions> options) =>
	{
		var json = await new StreamReader(httpRequest.Body).ReadToEndAsync();
		var signature = httpRequest.Headers["Stripe-Signature"].ToString();
		var webhookSecret = options.Value.WebhookSecret;

		if (string.IsNullOrWhiteSpace(webhookSecret))
			return Results.Problem("Stripe webhook secret is not configured.", statusCode: 500);

		Event stripeEvent;

		try
		{
			stripeEvent = EventUtility.ConstructEvent(json, signature, webhookSecret);
		}
		catch (StripeException ex)
		{
			return Results.BadRequest($"Invalid Stripe webhook signature: {ex.Message}");
		}

		if (stripeEvent.Data.Object is PaymentIntent intent)
		{
			await paymentService.UpdatePaymentFromStripeIntentAsync(intent);
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

	return Results.Ok(payment);
});

app.MapGet("/payments/appointment/{appointmentId:guid}",
async (Guid appointmentId, IPaymentService paymentService) =>
{
	var payment = await paymentService.GetPaymentByAppointmentAsync(appointmentId);

	if (payment is null)
		return Results.NotFound();

	return Results.Ok(payment);
});

app.MapGet("/payments",
async (IPaymentService paymentService) =>
{
	var payments = await paymentService.GetAllPaymentsAsync();
	return Results.Ok(payments);
});

app.Run();

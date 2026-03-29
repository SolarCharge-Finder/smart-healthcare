using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using PaymentService.Data;
using PaymentService.Models;
using Stripe;

namespace PaymentService.Services;

public interface IPaymentService
{
	Task<Payment> CreatePaymentIntentAsync(CreatePaymentIntentRequest request, CancellationToken cancellationToken = default);
	Task<Payment?> GetPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default);
	Task<Payment?> GetPaymentByAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<Payment>> GetAllPaymentsAsync(CancellationToken cancellationToken = default);
	Task<Payment?> ConfirmPaymentAsync(Guid paymentId, ConfirmPaymentRequest request, CancellationToken cancellationToken = default);
	Task<Payment?> UpdatePaymentFromStripeIntentAsync(PaymentIntent intent, CancellationToken cancellationToken = default);
}

public class PaymentProcessorService : IPaymentService
{
	private readonly PaymentDbContext _dbContext;
	private readonly PaymentIntentService _paymentIntentService;
	private readonly StripeOptions _stripeOptions;
	private readonly IAppointmentPricingService _appointmentPricingService;
	private readonly ILogger<PaymentProcessorService> _logger;

	public PaymentProcessorService(
		PaymentDbContext dbContext,
		PaymentIntentService paymentIntentService,
		IAppointmentPricingService appointmentPricingService,
		ILogger<PaymentProcessorService> logger,
		IOptions<StripeOptions> stripeOptions)
	{
		_dbContext = dbContext;
		_paymentIntentService = paymentIntentService;
		_appointmentPricingService = appointmentPricingService;
		_logger = logger;
		_stripeOptions = stripeOptions.Value;
	}

	public async Task<Payment> CreatePaymentIntentAsync(CreatePaymentIntentRequest request, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(_stripeOptions.SecretKey))
		{
			throw new InvalidOperationException("Stripe secret key is not configured.");
		}

		var pricing = await _appointmentPricingService.ResolvePricingAsync(
			request.AppointmentId,
			cancellationToken);

		var normalizedCurrency = pricing.Currency.ToLowerInvariant();

		var existing = await _dbContext.Payments
			.FirstOrDefaultAsync(p => p.AppointmentId == request.AppointmentId, cancellationToken);

		if (existing is not null)
		{
			if (existing.Status is PaymentStatus.Succeeded or PaymentStatus.Pending or PaymentStatus.Processing or PaymentStatus.RequiresAction)
			{
				return existing;
			}

			var recreated = await CreateStripePaymentIntentAsync(
				request.AppointmentId,
				pricing.Amount,
				normalizedCurrency,
				cancellationToken);

			existing.StripePaymentIntentId = recreated.Id;
			existing.ClientSecret = recreated.ClientSecret ?? existing.ClientSecret;
			existing.Amount = pricing.Amount;
			existing.Currency = normalizedCurrency;
			existing.Status = MapStripeStatus(recreated.Status);
			existing.FailureReason = recreated.LastPaymentError?.Message;
			existing.UpdatedAt = DateTime.UtcNow;

			await _dbContext.SaveChangesAsync(cancellationToken);
			return existing;
		}

		var paymentIntent = await CreateStripePaymentIntentAsync(
			request.AppointmentId,
			pricing.Amount,
			normalizedCurrency,
			cancellationToken);

		var payment = new Payment
		{
			Id = Guid.NewGuid(),
			AppointmentId = request.AppointmentId,
			StripePaymentIntentId = paymentIntent.Id,
			ClientSecret = paymentIntent.ClientSecret ?? string.Empty,
			Amount = pricing.Amount,
			Currency = normalizedCurrency,
			Status = MapStripeStatus(paymentIntent.Status),
			FailureReason = paymentIntent.LastPaymentError?.Message,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};

		_dbContext.Payments.Add(payment);
		await _dbContext.SaveChangesAsync(cancellationToken);

		return payment;
	}

	public async Task<Payment?> GetPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default)
	{
		return await _dbContext.Payments
			.AsNoTracking()
			.FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);
	}

	public async Task<Payment?> GetPaymentByAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default)
	{
		return await _dbContext.Payments
			.AsNoTracking()
			.FirstOrDefaultAsync(p => p.AppointmentId == appointmentId, cancellationToken);
	}

	public async Task<IReadOnlyList<Payment>> GetAllPaymentsAsync(CancellationToken cancellationToken = default)
	{
		return await _dbContext.Payments
			.AsNoTracking()
			.OrderByDescending(p => p.CreatedAt)
			.ToListAsync(cancellationToken);
	}

	public async Task<Payment?> ConfirmPaymentAsync(Guid paymentId, ConfirmPaymentRequest request, CancellationToken cancellationToken = default)
	{
		var payment = await _dbContext.Payments
			.FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);

		if (payment is null)
		{
			return null;
		}

		payment.Status = request.IsSuccess
			? PaymentStatus.Succeeded
			: PaymentStatus.Failed;
		payment.FailureReason = request.IsSuccess ? null : request.FailureReason;
		payment.UpdatedAt = DateTime.UtcNow;

		await _dbContext.SaveChangesAsync(cancellationToken);
		return payment;
	}

	public async Task<Payment?> UpdatePaymentFromStripeIntentAsync(PaymentIntent intent, CancellationToken cancellationToken = default)
	{
		var payment = await _dbContext.Payments
			.FirstOrDefaultAsync(p => p.StripePaymentIntentId == intent.Id, cancellationToken);

		if (payment is null)
		{
			_logger.LogWarning(
				"No payment found for StripePaymentIntentId {StripePaymentIntentId}.",
				intent.Id);
			return null;
		}

		var incomingStatus = MapStripeStatus(intent.Status);

		// Stripe webhooks can be delivered out of order; don't regress terminal states.
		if (IsTerminal(payment.Status) && !string.Equals(payment.Status, incomingStatus, StringComparison.Ordinal))
		{
			_logger.LogInformation(
				"Ignoring webhook status regression for payment {PaymentId}: current={CurrentStatus}, incoming={IncomingStatus}.",
				payment.Id,
				payment.Status,
				incomingStatus);

			return payment;
		}

		payment.Status = incomingStatus;
		payment.FailureReason = intent.LastPaymentError?.Message;
		payment.ClientSecret = intent.ClientSecret ?? payment.ClientSecret;
		payment.UpdatedAt = DateTime.UtcNow;

		await _dbContext.SaveChangesAsync(cancellationToken);

		_logger.LogInformation(
			"Payment {PaymentId} updated from webhook to status {Status}.",
			payment.Id,
			payment.Status);

		return payment;
	}

	private static bool IsTerminal(string status)
	{
		return string.Equals(status, PaymentStatus.Succeeded, StringComparison.Ordinal) ||
			string.Equals(status, PaymentStatus.Failed, StringComparison.Ordinal) ||
			string.Equals(status, PaymentStatus.Cancelled, StringComparison.Ordinal);
	}

	private async Task<PaymentIntent> CreateStripePaymentIntentAsync(
		Guid appointmentId,
		long amount,
		string normalizedCurrency,
		CancellationToken cancellationToken)
	{
		var options = new PaymentIntentCreateOptions
		{
			Amount = amount,
			Currency = normalizedCurrency,
			AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
			{
				Enabled = true
			},
			Metadata = new Dictionary<string, string>
			{
				["appointmentId"] = appointmentId.ToString()
			}
		};

		var requestOptions = new RequestOptions
		{
			IdempotencyKey = $"payment-intent:{appointmentId}:{amount}:{normalizedCurrency}"
		};

		return await _paymentIntentService.CreateAsync(
			options,
			requestOptions,
			cancellationToken);
	}

	private static string MapStripeStatus(string? stripeStatus)
	{
		return stripeStatus switch
		{
			"requires_payment_method" => PaymentStatus.Pending,
			"requires_confirmation" => PaymentStatus.Pending,
			"requires_action" => PaymentStatus.RequiresAction,
			"processing" => PaymentStatus.Processing,
			"succeeded" => PaymentStatus.Succeeded,
			"canceled" => PaymentStatus.Cancelled,
			_ => PaymentStatus.Failed
		};
	}
}

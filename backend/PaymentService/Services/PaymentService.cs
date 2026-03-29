using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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

	public PaymentProcessorService(
		PaymentDbContext dbContext,
		PaymentIntentService paymentIntentService,
		IOptions<StripeOptions> stripeOptions)
	{
		_dbContext = dbContext;
		_paymentIntentService = paymentIntentService;
		_stripeOptions = stripeOptions.Value;
	}

	public async Task<Payment> CreatePaymentIntentAsync(CreatePaymentIntentRequest request, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(_stripeOptions.SecretKey))
		{
			throw new InvalidOperationException("Stripe secret key is not configured.");
		}

		var normalizedCurrency = request.Currency.ToLowerInvariant();

		var existing = await _dbContext.Payments
			.FirstOrDefaultAsync(p => p.AppointmentId == request.AppointmentId, cancellationToken);

		if (existing is not null)
		{
			if (existing.Status is PaymentStatus.Succeeded or PaymentStatus.Pending or PaymentStatus.Processing or PaymentStatus.RequiresAction)
			{
				return existing;
			}

			var recreated = await CreateStripePaymentIntentAsync(request, normalizedCurrency, cancellationToken);

			existing.StripePaymentIntentId = recreated.Id;
			existing.ClientSecret = recreated.ClientSecret ?? existing.ClientSecret;
			existing.Amount = request.Amount;
			existing.Currency = normalizedCurrency;
			existing.Status = MapStripeStatus(recreated.Status);
			existing.FailureReason = recreated.LastPaymentError?.Message;
			existing.UpdatedAt = DateTime.UtcNow;

			await _dbContext.SaveChangesAsync(cancellationToken);
			return existing;
		}

		var paymentIntent = await CreateStripePaymentIntentAsync(request, normalizedCurrency, cancellationToken);

		var payment = new Payment
		{
			Id = Guid.NewGuid(),
			AppointmentId = request.AppointmentId,
			StripePaymentIntentId = paymentIntent.Id,
			ClientSecret = paymentIntent.ClientSecret ?? string.Empty,
			Amount = request.Amount,
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
			return null;
		}

		payment.Status = MapStripeStatus(intent.Status);
		payment.FailureReason = intent.LastPaymentError?.Message;
		payment.ClientSecret = intent.ClientSecret ?? payment.ClientSecret;
		payment.UpdatedAt = DateTime.UtcNow;

		await _dbContext.SaveChangesAsync(cancellationToken);
		return payment;
	}

	private async Task<PaymentIntent> CreateStripePaymentIntentAsync(
		CreatePaymentIntentRequest request,
		string normalizedCurrency,
		CancellationToken cancellationToken)
	{
		var options = new PaymentIntentCreateOptions
		{
			Amount = request.Amount,
			Currency = normalizedCurrency,
			AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
			{
				Enabled = true
			},
			Metadata = new Dictionary<string, string>
			{
				["appointmentId"] = request.AppointmentId.ToString()
			}
		};

		var requestOptions = new RequestOptions
		{
			IdempotencyKey = $"payment-intent:{request.AppointmentId}:{request.Amount}:{normalizedCurrency}"
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

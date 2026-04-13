using System.Net;
using System.Net.Http.Json;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using PaymentService.Models;

namespace PaymentService.Services;

public interface IAppointmentPricingService
{
    Task<(long Amount, string Currency)> ResolvePricingAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default);
}

public class AppointmentPricingService : IAppointmentPricingService
{
    private readonly HttpClient _httpClient;
    private readonly PaymentPricingOptions _pricingOptions;
    private readonly ILogger<AppointmentPricingService> _logger;

    public AppointmentPricingService(
        HttpClient httpClient,
        IOptions<PaymentPricingOptions> pricingOptions,
        ILogger<AppointmentPricingService> logger)
    {
        _httpClient = httpClient;
        _pricingOptions = pricingOptions.Value;
        _logger = logger;
    }

    public async Task<(long Amount, string Currency)> ResolvePricingAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        AppointmentLookupResponse? appointment;
        var appointmentServiceUrl = $"{_httpClient.BaseAddress}appointments/{appointmentId}";

        try
        {
            _logger.LogInformation(
                "Attempting to fetch appointment pricing from {AppointmentServiceUrl}",
                appointmentServiceUrl);

            appointment = await _httpClient.GetFromJsonAsync<AppointmentLookupResponse>(
                $"appointments/{appointmentId}",
                cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning(
                ex,
                "Appointment {AppointmentId} not found at {AppointmentServiceUrl}",
                appointmentId,
                appointmentServiceUrl);
            throw new KeyNotFoundException($"Appointment {appointmentId} not found.", ex);
        }
        catch (HttpRequestException ex) when (ex.StatusCode.HasValue)
        {
            _logger.LogError(
                ex,
                "Appointment service returned error {StatusCode} for {AppointmentServiceUrl}",
                ex.StatusCode,
                appointmentServiceUrl);
            throw new InvalidOperationException(
                $"Appointment service error (HTTP {ex.StatusCode}): Unable to fetch appointment details.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Failed to connect to appointment service at {AppointmentServiceUrl}. Service may be unavailable.",
                appointmentServiceUrl);

            // Development fallback: use default pricing if development mode is enabled
            if (_pricingOptions.DevFallbackEnabled)
            {
                _logger.LogWarning(
                    "Appointment service unavailable. Using development fallback pricing for appointment {AppointmentId}.",
                    appointmentId);
                return GetFallbackPricing();
            }

            throw new InvalidOperationException(
                $"Appointment service unavailable at {_httpClient.BaseAddress}. Cannot fetch appointment details.", ex);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(
                ex,
                "Request to appointment service timed out for {AppointmentServiceUrl}",
                appointmentServiceUrl);
            throw new InvalidOperationException(
                "Appointment service request timed out. Please try again.", ex);
        }

        if (appointment is null)
        {
            _logger.LogWarning(
                "Appointment service returned empty response for appointment {AppointmentId}",
                appointmentId);
            throw new KeyNotFoundException($"Appointment {appointmentId} not found.");
        }

        _logger.LogInformation(
            "Successfully fetched pricing for appointment {AppointmentId} (Doctor: {DoctorId})",
            appointmentId,
            appointment.DoctorId);

        return ResolvePricingFromAppointment(appointment);
    }

    private (long Amount, string Currency) ResolvePricingFromAppointment(AppointmentLookupResponse appointment)
    {
        if (appointment.TotalFee > 0)
        {
            var computedAmount = ConvertToMinorUnit(appointment.TotalFee);
            var currencyFromAppointment = string.IsNullOrWhiteSpace(appointment.Currency)
                ? _pricingOptions.Currency
                : appointment.Currency;

            var normalizedCurrency = string.IsNullOrWhiteSpace(currencyFromAppointment)
                ? "lkr"
                : currencyFromAppointment.ToLowerInvariant();

            _logger.LogInformation(
                "Using appointment total fee {TotalFee} ({Amount} minor units) for appointment {AppointmentId}",
                appointment.TotalFee,
                computedAmount,
                appointment.Id);

            return (computedAmount, normalizedCurrency);
        }

        var amount = _pricingOptions.DefaultAmount;

        if (_pricingOptions.DoctorFees.TryGetValue(appointment.DoctorId.ToString(), out var doctorFee)
            && doctorFee > 0)
        {
            _logger.LogInformation(
                "Using doctor fee {Amount} for doctor {DoctorId}",
                doctorFee,
                appointment.DoctorId);
            amount = doctorFee;
        }
        else
        {
            _logger.LogInformation(
                "Using default amount {Amount} for doctor {DoctorId}",
                amount,
                appointment.DoctorId);
        }

        if (amount <= 0)
        {
            throw new InvalidOperationException(
                $"Resolved payment amount is invalid (amount={amount}). Check PaymentPricing configuration.");
        }

        var currency = string.IsNullOrWhiteSpace(_pricingOptions.Currency)
            ? "lkr"
            : _pricingOptions.Currency;

        return (amount, currency);
    }

    private static long ConvertToMinorUnit(decimal majorAmount)
    {
        // Stripe expects amount in the smallest currency unit (e.g., cents).
        return (long)Math.Round(majorAmount * 100m, 0, MidpointRounding.AwayFromZero);
    }

    private (long Amount, string Currency) GetFallbackPricing()
    {
        var amount = _pricingOptions.DefaultAmount > 0
            ? _pricingOptions.DefaultAmount
            : 50000; // LKR 500.00 in cents as ultimate fallback

        var currency = string.IsNullOrWhiteSpace(_pricingOptions.Currency)
            ? "lkr"
            : _pricingOptions.Currency;

        _logger.LogWarning(
            "Using fallback pricing: {Amount} {Currency}",
            amount,
            currency);

        return (amount, currency);
    }

    private sealed class AppointmentLookupResponse
    {
        public Guid Id { get; set; }
        public Guid DoctorId { get; set; }
        public decimal TotalFee { get; set; }
        public string? Currency { get; set; }
    }
}

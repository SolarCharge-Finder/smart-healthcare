using System.Net;
using System.Net.Http.Json;
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

    public AppointmentPricingService(
        HttpClient httpClient,
        IOptions<PaymentPricingOptions> pricingOptions)
    {
        _httpClient = httpClient;
        _pricingOptions = pricingOptions.Value;
    }

    public async Task<(long Amount, string Currency)> ResolvePricingAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        AppointmentLookupResponse? appointment;

        try
        {
            appointment = await _httpClient.GetFromJsonAsync<AppointmentLookupResponse>(
                $"appointments/{appointmentId}",
                cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new KeyNotFoundException("Appointment not found.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException(
                "Failed to fetch appointment details for pricing.", ex);
        }

        if (appointment is null)
        {
            throw new KeyNotFoundException("Appointment not found.");
        }

        var amount = _pricingOptions.DefaultAmount;

        if (_pricingOptions.DoctorFees.TryGetValue(appointment.DoctorId.ToString(), out var doctorFee)
            && doctorFee > 0)
        {
            amount = doctorFee;
        }

        if (amount <= 0)
        {
            throw new InvalidOperationException("Resolved payment amount is invalid.");
        }

        var currency = string.IsNullOrWhiteSpace(_pricingOptions.Currency)
            ? "lkr"
            : _pricingOptions.Currency;

        return (amount, currency);
    }

    private sealed class AppointmentLookupResponse
    {
        public Guid Id { get; set; }
        public Guid DoctorId { get; set; }
    }
}

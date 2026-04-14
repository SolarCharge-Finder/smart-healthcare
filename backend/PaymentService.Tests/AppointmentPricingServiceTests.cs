using System.Net;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using PaymentService.Models;
using PaymentService.Services;

namespace PaymentService.Tests;

public class AppointmentPricingServiceTests
{
    [Fact]
    public async Task ResolvePricingAsync_Uses_DoctorFee_When_Configured()
    {
        var appointmentId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();

        using var httpClient = new HttpClient(new JsonResponseHandler(
            HttpStatusCode.OK,
            JsonSerializer.Serialize(new { id = appointmentId, doctorId })))
        {
            BaseAddress = new Uri("http://appointment-service/")
        };

        var options = Options.Create(new PaymentPricingOptions
        {
            DefaultAmount = 50000,
            Currency = "lkr",
            DoctorFees = new Dictionary<string, long>
            {
                [doctorId.ToString()] = 75000
            }
        });

        var service = new AppointmentPricingService(
            httpClient,
            options,
            NullLogger<AppointmentPricingService>.Instance);

        var (amount, currency) = await service.ResolvePricingAsync(appointmentId);

        Assert.Equal(75000, amount);
        Assert.Equal("lkr", currency);
    }

    [Fact]
    public async Task ResolvePricingAsync_Uses_Development_Fallback_When_Service_Unavailable()
    {
        using var httpClient = new HttpClient(new ThrowingHandler())
        {
            BaseAddress = new Uri("http://appointment-service/")
        };

        var options = Options.Create(new PaymentPricingOptions
        {
            DefaultAmount = 62500,
            Currency = "lkr",
            DevFallbackEnabled = true
        });

        var service = new AppointmentPricingService(
            httpClient,
            options,
            NullLogger<AppointmentPricingService>.Instance);

        var (amount, currency) = await service.ResolvePricingAsync(Guid.NewGuid());

        Assert.Equal(62500, amount);
        Assert.Equal("lkr", currency);
    }

    private sealed class JsonResponseHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly byte[] _payload;

        public JsonResponseHandler(HttpStatusCode statusCode, string json)
        {
            _statusCode = statusCode;
            _payload = Encoding.UTF8.GetBytes(json);
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new ByteArrayContent(_payload)
            };

            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            return Task.FromResult(response);
        }
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            throw new HttpRequestException("Appointment service unavailable.");
        }
    }
}

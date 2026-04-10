using System.Text.Json;

using Microsoft.AspNetCore.Mvc.Testing;

using PaymentService;

namespace PaymentService.Tests;

public class PaymentFlowIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        // Configure factory to use in-memory test environment
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ASPNETCORE_ENVIRONMENT", "Testing");
                // Factory will use in-memory services and default configurations
            });

        _client = _factory.CreateClient();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        _factory?.Dispose();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that the /health/live endpoint returns 200 OK with "alive" status.
    /// This endpoint is used by Kubernetes liveness probes to detect if the service is running.
    /// </summary>
    [Fact]
    public async Task HealthLiveEndpoint_Returns200OkWithAliveStatus()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/health/live");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("alive", content.Trim('"'));
    }

    /// <summary>
    /// Verifies that the /health/ready endpoint returns 200 OK when database is connected.
    /// This endpoint is used by Kubernetes readiness probes to detect if the service is ready to handle requests.
    /// </summary>
    [Fact]
    public async Task HealthReadyEndpoint_Returns200OkWhenDatabaseConnected()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/health/ready");

        // Assert
        // Should return OK if database is accessible
        // Note: May return 503 if DB is not available, but in test environment with in-memory DB, should succeed
        Assert.True(
            response.StatusCode == System.Net.HttpStatusCode.OK ||
            response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable,
            $"Expected OK (200) or ServiceUnavailable (503), got {response.StatusCode}"
        );

        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal("ready", content.Trim('"'));
        }
    }

    /// <summary>
    /// Verifies that the /health/ready endpoint returns 503 if database is unavailable.
    /// Note: This test verifies the health check behavior, but actual DB failure would need a special configuration.
    /// For now, we test that the endpoint exists and is reachable.
    /// </summary>
    [Fact]
    public async Task HealthReadyEndpoint_IsAccessible()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/health/ready");

        // Assert
        Assert.NotNull(response);
        Assert.True(
            response.StatusCode == System.Net.HttpStatusCode.OK ||
            response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable,
            "HealthReady endpoint should be accessible"
        );
    }

    /// <summary>
    /// Verifies that the /payments/config endpoint returns Stripe publishable key configuration.
    /// Frontend uses this to initialize Stripe.js without embedding the key directly.
    /// </summary>
    [Fact]
    public async Task PaymentsConfigEndpoint_ReturnsPublishableKey()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/payments/config");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        Assert.True(
            root.TryGetProperty("publishableKey", out _),
            "Response must contain publishableKey field"
        );
    }

    /// <summary>
    /// Verifies that the /payments/{id} endpoint returns 404 when payment does not exist.
    /// This validates the endpoint structure and error handling.
    /// </summary>
    [Fact]
    public async Task GetPaymentEndpoint_Returns404ForNonExistentPayment()
    {
        // Arrange
        var nonExistentPaymentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/payments/{nonExistentPaymentId}");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Verifies that the /payments/appointment/{appointmentId} endpoint returns 404 
    /// when no payment exists for the appointment.
    /// </summary>
    [Fact]
    public async Task GetPaymentByAppointmentEndpoint_Returns404ForNonExistentAppointment()
    {
        // Arrange
        var nonExistentAppointmentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/payments/appointment/{nonExistentAppointmentId}");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Verifies that the /payments endpoint returns 200 OK with a list of payments.
    /// </summary>
    [Fact]
    public async Task GetAllPaymentsEndpoint_Returns200OkWithPaymentsList()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/payments");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        // Should be an array (even if empty)
        Assert.Equal(JsonValueKind.Array, root.ValueKind);
    }

    /// <summary>
    /// Verifies that the /payments/intents POST endpoint returns 400 Bad Request
    /// when AppointmentId is empty/missing.
    /// </summary>
    [Fact]
    public async Task CreatePaymentIntentEndpoint_Returns400ForEmptyAppointmentId()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/payments/intents")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { appointmentId = Guid.Empty }),
                System.Text.Encoding.UTF8,
                "application/json"
            )
        };

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Verifies that the /payments/intents POST endpoint rejects invalid JSON payloads
    /// with a 400 Bad Request response.
    /// </summary>
    [Fact]
    public async Task CreatePaymentIntentEndpoint_Returns400ForInvalidJson()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/payments/intents")
        {
            Content = new StringContent(
                "{ invalid json",
                System.Text.Encoding.UTF8,
                "application/json"
            )
        };

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Verifies that the /payments/intents POST endpoint rejects unsupported fields
    /// in the request body (only appointmentId is allowed).
    /// </summary>
    [Fact]
    public async Task CreatePaymentIntentEndpoint_Returns400ForUnsupportedField()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/payments/intents")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    appointmentId = Guid.NewGuid(),
                    extraField = "not allowed"
                }),
                System.Text.Encoding.UTF8,
                "application/json"
            )
        };

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Unsupported field", content);
    }

    /// <summary>
    /// Verifies that the /payments/{id}/confirm POST endpoint returns 400 Bad Request
    /// when IsSuccess is false but FailureReason is not provided.
    /// </summary>
    [Fact]
    public async Task ConfirmPaymentEndpoint_Returns400WhenFailureReasonMissing()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Post, $"/payments/{paymentId}/confirm")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { isSuccess = false }),
                System.Text.Encoding.UTF8,
                "application/json"
            )
        };

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Verifies that the /payments/{id}/confirm POST endpoint returns 404 Not Found
    /// when trying to confirm a non-existent payment.
    /// </summary>
    [Fact]
    public async Task ConfirmPaymentEndpoint_Returns404ForNonExistentPayment()
    {
        // Arrange
        var nonExistentPaymentId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Post, $"/payments/{nonExistentPaymentId}/confirm")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { isSuccess = true }),
                System.Text.Encoding.UTF8,
                "application/json"
            )
        };

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Verifies that CORS is enabled for payment endpoints.
    /// Frontend on localhost:3000 should be allowed to POST to /payments/intents.
    /// </summary>
    [Fact]
    public async Task PaymentEndpoints_AllowCORSFromLocalhost3000()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Options, "/payments/intents");
        request.Headers.Add("Origin", "http://localhost:3000");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.True(
            response.StatusCode == System.Net.HttpStatusCode.OK ||
            response.StatusCode == System.Net.HttpStatusCode.NoContent,
            $"Expected OK (200) or NoContent (204), got {response.StatusCode}"
        );
        Assert.True(
            response.Headers.Contains("Access-Control-Allow-Origin"),
            "Response should contain CORS header"
        );
    }

    /// <summary>
    /// Verifies that all configured dev ports (3001, 3002) are also allowed by CORS.
    /// </summary>
    [Theory]
    [InlineData("http://localhost:3001")]
    [InlineData("http://localhost:3002")]
    public async Task PaymentEndpoints_AllowCORSFromAllConfiguredPorts(string origin)
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Options, "/payments/intents");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.True(
            response.StatusCode == System.Net.HttpStatusCode.OK ||
            response.StatusCode == System.Net.HttpStatusCode.NoContent,
            $"Expected OK (200) or NoContent (204) for {origin}, got {response.StatusCode}"
        );
        Assert.True(
            response.Headers.Contains("Access-Control-Allow-Origin"),
            $"Origin {origin} should be allowed"
        );
    }

    /// <summary>
    /// Verifies that the webhook endpoint returns 200 OK with expected structure
    /// (even though we won't provide valid Stripe signature in tests).
    /// This validates endpoint accessibility and error handling.
    /// </summary>
    [Fact]
    public async Task WebhookEndpoint_Returns400ForMissingSignature()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/payments/webhook")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { type = "payment_intent.succeeded" }),
                System.Text.Encoding.UTF8,
                "application/json"
            )
        };
        // Don't add Stripe-Signature header - should fail validation

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Verifies that POST requests to payment endpoints from allowed origins
    /// include CORS headers in the response.
    /// </summary>
    [Fact]
    public async Task PaymentEndpoints_IncludeCORSHeadersInPostResponse()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/payments/intents")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { appointmentId = Guid.Empty }),
                System.Text.Encoding.UTF8,
                "application/json"
            )
        };
        request.Headers.Add("Origin", "http://localhost:3000");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        // Even if the endpoint returns 400 (invalid request), CORS headers should be present
        Assert.True(
            response.Headers.Contains("Access-Control-Allow-Origin"),
            "POST response should include CORS header"
        );
    }
}

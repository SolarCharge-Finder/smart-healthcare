using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using TelemedicineService;

namespace TelemedicineService.Tests;

public class ConsultationFlowIntegrationTests : IAsyncLifetime
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
    /// Verifies that CORS preflight (OPTIONS) request to /telemedicine/session
    /// from http://localhost:3000 returns correct Access-Control headers.
    /// </summary>
    [Fact]
    public async Task CORSPreflight_FromLocalhost3000_ReturnsAllowedHeaders()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Options, "/telemedicine/session");
        request.Headers.Add("Origin", "http://localhost:3000");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "Content-Type");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        // CORS preflight can return 200 or 204
        Assert.True(
            response.StatusCode == System.Net.HttpStatusCode.OK ||
            response.StatusCode == System.Net.HttpStatusCode.NoContent,
            $"Expected OK (200) or NoContent (204), got {response.StatusCode}"
        );
        Assert.True(
            response.Headers.Contains("Access-Control-Allow-Origin"),
            "Response must contain Access-Control-Allow-Origin header"
        );
        Assert.Equal(
            "http://localhost:3000",
            response.Headers.GetValues("Access-Control-Allow-Origin").First()
        );
        Assert.True(
            response.Headers.Contains("Access-Control-Allow-Methods"),
            "Response must contain Access-Control-Allow-Methods header"
        );
        Assert.True(
            response.Headers.Contains("Access-Control-Allow-Headers"),
            "Response must contain Access-Control-Allow-Headers header"
        );
    }

    /// <summary>
    /// Verifies that CORS preflight request from http://127.0.0.1:3000 (alternative localhost)
    /// is also accepted by the configuration.
    /// </summary>
    [Fact]
    public async Task CORSPreflight_From127_0_0_1_Port3000_ReturnsAllowedHeaders()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Options, "/telemedicine/session");
        request.Headers.Add("Origin", "http://127.0.0.1:3000");
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
            "Response must contain Access-Control-Allow-Origin header for 127.0.0.1:3000"
        );
        Assert.Equal(
            "http://127.0.0.1:3000",
            response.Headers.GetValues("Access-Control-Allow-Origin").First()
        );
    }

    /// <summary>
    /// Verifies that CORS preflight request from non-allowed origin (e.g., http://example.com)
    /// is rejected (no Access-Control-Allow-Origin header returned).
    /// </summary>
    [Fact]
    public async Task CORSPreflight_FromUnallowedOrigin_RejectsRequest()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Options, "/telemedicine/session");
        request.Headers.Add("Origin", "http://example.com");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        // Act
        var response = await _client.SendAsync(request);

        // Assert - CORS rejection means no Allow-Origin header
        Assert.False(
            response.Headers.Contains("Access-Control-Allow-Origin"),
            "Requests from unallowed origins should not receive Access-Control-Allow-Origin header"
        );
    }

    /// <summary>
    /// Verifies that actual POST requests to /telemedicine/session include CORS headers
    /// in the response when Origin header is provided from allowed source.
    /// </summary>
    [Fact]
    public async Task POSTRequest_WithAllowedOrigin_IncludesCORSHeaders()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/telemedicine/session")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { appointmentId = Guid.NewGuid() }),
                System.Text.Encoding.UTF8,
                "application/json"
            )
        };
        request.Headers.Add("Origin", "http://localhost:3000");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        // Response should have CORS headers even if endpoint returns 404 (appointment doesn't exist)
        // The CORS policy is applied before the endpoint handler
        Assert.True(
            response.Headers.Contains("Access-Control-Allow-Origin"),
            "POST response must include Access-Control-Allow-Origin header"
        );
        Assert.Equal(
            "http://localhost:3000",
            response.Headers.GetValues("Access-Control-Allow-Origin").First()
        );
    }

    /// <summary>
    /// Verifies that the /health endpoint returns 200 OK status with correct response shape.
    /// This endpoint is used by Kubernetes liveness and readiness probes.
    /// </summary>
    [Fact]
    public async Task HealthEndpoint_Returns200OkWithHealthStatus()
    {
        // Arrange
        var expectedContentType = "application/json";

        // Act
        var response = await _client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(expectedContentType, response.Content.Headers.ContentType?.MediaType ?? string.Empty);

        // Verify response body contains expected fields
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        Assert.True(root.TryGetProperty("status", out var statusProp), "Response must contain 'status' field");
        Assert.Equal("healthy", statusProp.GetString());

        Assert.True(root.TryGetProperty("service", out var serviceProp), "Response must contain 'service' field");
        Assert.NotNull(serviceProp.GetString());
    }

    /// <summary>
    /// Verifies that the /health endpoint does not require CORS headers
    /// (it's a public liveness/readiness probe endpoint).
    /// </summary>
    [Fact]
    public async Task HealthEndpoint_AccessibleWithoutCORSHeaders()
    {
        // Arrange - no Origin header

        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("healthy", content);
    }

    /// <summary>
    /// Verifies that additional allowed origins (localhost:3001, localhost:3002) are also configured.
    /// </summary>
    [Theory]
    [InlineData("http://localhost:3001")]
    [InlineData("http://127.0.0.1:3001")]
    [InlineData("http://localhost:3002")]
    [InlineData("http://127.0.0.1:3002")]
    public async Task CORSPreflight_AllConfiguredPorts_AreAllowed(string origin)
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Options, "/telemedicine/session");
        request.Headers.Add("Origin", origin);
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
            $"Origin {origin} should be allowed"
        );
        Assert.Equal(
            origin,
            response.Headers.GetValues("Access-Control-Allow-Origin").First()
        );
    }
}

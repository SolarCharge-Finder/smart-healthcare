using System.Net;
using System.Net.Http.Json;
using AIService.DTOs;
using AIService.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using FluentAssertions;

namespace AIService.Tests.Integration;

public class AiServiceIntegrationTests : IAsyncLifetime
{
    private HttpClient? _httpClient;
    private WebApplicationFactory<Program>? _factory;
    private Mock<IOpenAIService>? _mockOpenAIService;

    public async Task InitializeAsync()
    {
        _mockOpenAIService = new Mock<IOpenAIService>();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    // Replace the real OpenAIService with a mock
                    var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IOpenAIService));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }
                    services.AddScoped(_ => _mockOpenAIService!.Object);
                });
            });

        _httpClient = _factory.CreateClient();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _httpClient?.Dispose();
        _factory?.Dispose();
        await Task.CompletedTask;
    }

    [Fact]
    public async Task AnalyzeEndpoint_WithValidSymptoms_ShouldReturnAnalysis()
    {
        // Arrange
        var symptoms = "I have a severe headache and fever for 2 days";
        var expectedResponse = new OpenAIResponse
        {
            IsSuccess = true,
            Content = "Possible viral infection. Recommend rest and hydration.",
            TokensUsed = 150,
            CostUsd = 0.005m,
            ModelUsed = "gpt-4o",
            ResponseTimeMs = 1200,
            CorrelationId = Guid.NewGuid().ToString()
        };

        _mockOpenAIService!
            .Setup(s => s.AnalyzeSymptomsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var request = new SymptomAnalysisRequest { Symptoms = symptoms };

        // Act
        var response = await _httpClient!.PostAsJsonAsync("/api/ai/analyze", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SymptomAnalysisResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Analysis.Should().Contain("infection");
        _mockOpenAIService.Verify(s => s.AnalyzeSymptomsAsync(symptoms, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AnalyzeEndpoint_WithShortSymptoms_ShouldReturnError()
    {
        // Arrange - Symptoms shorter than minimum required length
        var request = new SymptomAnalysisRequest { Symptoms = "short" };

        // Act
        var response = await _httpClient!.PostAsJsonAsync("/api/ai/analyze", request);

        // Assert - Should return error status (validation fails in controller)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task AnalyzeEndpoint_WhenOpenAIFails_ShouldReturnError()
    {
        // Arrange
        var failureResponse = new OpenAIResponse
        {
            IsSuccess = false,
            ErrorMessage = "OpenAI API rate limited",
            CorrelationId = Guid.NewGuid().ToString()
        };

        _mockOpenAIService!
            .Setup(s => s.AnalyzeSymptomsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failureResponse);

        var request = new SymptomAnalysisRequest { Symptoms = "I have persistent chest pain and shortness of breath" };

        // Act
        var response = await _httpClient!.PostAsJsonAsync("/api/ai/analyze", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var result = await response.Content.ReadFromJsonAsync<SymptomAnalysisResponse>();
        result!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task AnalyzeEndpoint_ShouldRecordMetrics()
    {
        // Arrange
        var symptoms = "I experience recurring migraines with visual disturbances";
        var expectedResponse = new OpenAIResponse
        {
            IsSuccess = true,
            Content = "Test analysis",
            TokensUsed = 100,
            CostUsd = 0.003m,
            ModelUsed = "gpt-4o",
            ResponseTimeMs = 800,
            CorrelationId = Guid.NewGuid().ToString()
        };

        _mockOpenAIService!
            .Setup(s => s.AnalyzeSymptomsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var response = await _httpClient!.PostAsJsonAsync("/api/ai/analyze",
            new SymptomAnalysisRequest { Symptoms = symptoms });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify metrics endpoint returns data
        var metricsResponse = await _httpClient.GetAsync("/metrics");
        metricsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var metricsContent = await metricsResponse.Content.ReadAsStringAsync();
        metricsContent.Should().Contain("ai_analysis_total");
    }

    [Fact]
    public async Task HealthEndpoint_ShouldReturnOk()
    {
        // Act
        var response = await _httpClient!.GetAsync("/api/ai/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MetricsEndpoint_ShouldReturnPrometheusFormat()
    {
        // Act
        var response = await _httpClient!.GetAsync("/metrics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("# HELP");
        content.Should().Contain("# TYPE");
        content.Should().Contain("ai_analysis_total");
    }

    [Fact]
    public async Task MultipleRequests_ShouldAccumulateMetrics()
    {
        // Arrange
        var analysis = new OpenAIResponse
        {
            IsSuccess = true,
            Content = "Test analysis result",
            TokensUsed = 50,
            CostUsd = 0.001m,
            ModelUsed = "gpt-4o",
            ResponseTimeMs = 600,
            CorrelationId = Guid.NewGuid().ToString()
        };

        _mockOpenAIService!
            .Setup(s => s.AnalyzeSymptomsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysis);

        // Act - Make 3 requests
        for (int i = 0; i < 3; i++)
        {
            var response = await _httpClient!.PostAsJsonAsync("/api/ai/analyze",
                new SymptomAnalysisRequest { Symptoms = $"Patient reports symptoms {i}: persistent fatigue and joint pain" });
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // Assert - Check metrics accumulation
        var metricsResponse = await _httpClient!.GetAsync("/metrics");
        var metricsContent = await metricsResponse.Content.ReadAsStringAsync();
        // Metrics should show successful requests recorded
        metricsContent.Should().Contain("ai_analysis_total");
        metricsContent.Should().Contain("api_requests_total");
    }

    [Fact]
    public async Task AnalyzeEndpoint_FiltersComplex_ShouldWorkCorrectly()
    {
        // Arrange
        var complexSymptoms = "I've had a sore throat with white patches for 3 days, difficulty swallowing, and fever reaching 101°F. I also have swollen lymph nodes in my neck.";
        var response = new OpenAIResponse
        {
            IsSuccess = true,
            Content = "Signs consistent with strep throat or similar bacterial infection",
            TokensUsed = 200,
            CostUsd = 0.006m,
            ModelUsed = "gpt-4o",
            ResponseTimeMs = 1400,
            CorrelationId = Guid.NewGuid().ToString()
        };

        _mockOpenAIService!
            .Setup(s => s.AnalyzeSymptomsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _httpClient!.PostAsJsonAsync("/api/ai/analyze",
            new SymptomAnalysisRequest { Symptoms = complexSymptoms });

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var analysisResult = await result.Content.ReadFromJsonAsync<SymptomAnalysisResponse>();
        analysisResult!.Success.Should().BeTrue();
        analysisResult.CostUsd.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AnalyzeEndpoint_ShouldReturn429_WhenRateLimitExceeded()
    {
        // Arrange
        var analysis = new OpenAIResponse
        {
            IsSuccess = true,
            Content = "Test analysis",
            TokensUsed = 20,
            CostUsd = 0.001m,
            ModelUsed = "gemini-flash-latest",
            ResponseTimeMs = 200,
            CorrelationId = Guid.NewGuid().ToString()
        };

        _mockOpenAIService!
            .Setup(s => s.AnalyzeSymptomsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysis);

        // Act: Limit is 5/hour in middleware; 6th request should be blocked.
        HttpResponseMessage? lastResponse = null;
        for (var i = 0; i < 6; i++)
        {
            lastResponse = await _httpClient!.PostAsJsonAsync("/api/ai/analyze",
                new SymptomAnalysisRequest { Symptoms = $"Rate limit test symptom payload {i} with enough length" });
        }

        // Assert
        lastResponse.Should().NotBeNull();
        lastResponse!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task AnalyzeEndpoint_ResponseSchema_ShouldContainRequiredFields_AndNoUrgencyLevel()
    {
        // Arrange
        var aiResponse = new OpenAIResponse
        {
            IsSuccess = true,
            Content = "Migraine, Tension Headache",
            PossibleConditions = new List<string> { "Migraine", "Tension Headache" },
            ConfidenceScore = 0.84,
            RecommendedSpecialty = "Neurology",
            Urgency = "Medium",
            Disclaimer = "This is not medical advice. Consult a healthcare professional.",
            TokensUsed = 123,
            CostUsd = 0.001m,
            ModelUsed = "gemini-flash-latest",
            ResponseTimeMs = 450,
            CorrelationId = Guid.NewGuid().ToString()
        };

        _mockOpenAIService!
            .Setup(s => s.AnalyzeSymptomsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(aiResponse);

        // Act
        var response = await _httpClient!.PostAsJsonAsync("/api/ai/analyze",
            new SymptomAnalysisRequest { Symptoms = "Persistent headache with nausea and mild photophobia" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("\"possibleConditions\"");
        json.Should().Contain("\"confidenceScore\"");
        json.Should().Contain("\"recommendedSpecialty\"");
        json.Should().Contain("\"urgency\"");
        json.Should().Contain("\"disclaimer\"");
        json.Should().NotContain("\"urgencyLevel\"");
    }
}

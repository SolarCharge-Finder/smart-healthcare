using AIService.Services;
using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace AIService.Tests.Services;

public class FallbackServiceTests
{
    private readonly Mock<ILogger<FallbackService>> _mockLogger;
    private readonly FallbackService _service;

    public FallbackServiceTests()
    {
        _mockLogger = new Mock<ILogger<FallbackService>>();
        _service = new FallbackService(_mockLogger.Object);
    }

    [Fact]
    public void GetFallbackResponse_WithSymptoms_ShouldReturnValidResponse()
    {
        // Arrange
        var symptoms = "I have a severe chest pain and shortness of breath";
        var correlationId = Guid.NewGuid().ToString();

        // Act
        var response = _service.GetFallbackResponse(symptoms, correlationId);

        // Assert
        response.Should().NotBeNull();
        response.IsSuccess.Should().BeTrue();
        response.Content.Should().NotBeNullOrEmpty();
        // Chest symptoms match multiple conditions
        response.Content.Should().ContainAny(new[] { "Cardiology", "Angina", "Arrhythmia" });
        response.ModelUsed.Should().Be("fallback-local-analysis");
        response.CostUsd.Should().Be(0);
        response.CorrelationId.Should().Be(correlationId);
    }

    [Fact]
    public void GetFallbackResponse_WithFeverSymptoms_ShouldIdentifyInfection()
    {
        // Arrange
        var symptoms = "High fever and fatigue for 3 days";
        var correlationId = Guid.NewGuid().ToString();

        // Act
        var response = _service.GetFallbackResponse(symptoms, correlationId);

        // Assert
        response.Content.Should().Contain("Viral Infection");
        response.Content.Should().Contain("Infectious Disease");
    }

    [Fact]
    public void GetFallbackResponse_WithHeadacheSymptoms_ShouldIdentifyMigraine()
    {
        // Arrange
        var symptoms = "Severe migraine with visual disturbances";
        var correlationId = Guid.NewGuid().ToString();

        // Act
        var response = _service.GetFallbackResponse(symptoms, correlationId);

        // Assert
        response.Content.Should().Contain("Migraine");
        response.Content.Should().Contain("Neurology");
    }

    [Fact]
    public void ShouldUseFallback_WithHttpRequestException_ShouldReturnTrue()
    {
        // Arrange
        var ex = new HttpRequestException("API unavailable");

        // Act
        var result = _service.ShouldUseFallback(ex);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldUseFallback_WithTaskCanceledException_ShouldReturnTrue()
    {
        // Arrange
        var ex = new TaskCanceledException("Request timeout");

        // Act
        var result = _service.ShouldUseFallback(ex);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldUseFallback_WithTimeoutException_ShouldReturnTrue()
    {
        // Arrange
        var ex = new TimeoutException("Operation timed out");

        // Act
        var result = _service.ShouldUseFallback(ex);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldUseFallback_WithRateLimitException_ShouldReturnTrue()
    {
        // Arrange
        var ex = new Exception("Rate limit exceeded");

        // Act
        var result = _service.ShouldUseFallback(ex);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldUseFallback_WithOtherException_ShouldReturnFalse()
    {
        // Arrange
        var ex = new InvalidOperationException("Some other error");

        // Act
        var result = _service.ShouldUseFallback(ex);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetFallbackResponse_WithGeneralSymptoms_ShouldReturnFallback()
    {
        // Arrange
        var symptoms = "I'm not feeling well";
        var correlationId = Guid.NewGuid().ToString();

        // Act
        var response = _service.GetFallbackResponse(symptoms, correlationId);

        // Assert
        response.IsSuccess.Should().BeTrue();
        response.Content.Should().Contain("General Illness");
        response.Content.Should().Contain("Disclaimer");
    }
}

using AIService.Services;
using Xunit;
using FluentAssertions;
using System.Net;
using Moq;
using Microsoft.Extensions.Logging;

namespace AIService.Tests.Services;

public class ResilienceServiceTests : IDisposable
{
    private readonly IResilienceService _resilienceService;
    private readonly Mock<ILogger<ResilienceService>> _mockLogger;

    public ResilienceServiceTests()
    {
        _mockLogger = new Mock<ILogger<ResilienceService>>();
        _resilienceService = new ResilienceService(_mockLogger.Object);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    [Fact]
    public async Task ExecuteAsync_WithValidOperation_ShouldSucceed()
    {
        // Arrange
        var operation = new Func<Task<string>>(async () =>
        {
            await Task.Delay(10);
            return "success";
        });

        // Act
        var result = await _resilienceService.ExecuteAsync(operation);

        // Assert
        result.Should().Be("success");
    }

    [Fact]
    public async Task ExecuteAsync_WithTransientFailure_ShouldRetry()
    {
        // Arrange
        int attemptCount = 0;
        var operation = new Func<Task<string>>(async () =>
        {
            attemptCount++;
            if (attemptCount < 2)
            {
                throw new HttpRequestException("Transient failure");
            }
            await Task.Delay(10);
            return "success";
        });

        // Act
        var result = await _resilienceService.ExecuteAsync(operation);

        // Assert
        result.Should().Be("success");
        attemptCount.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task ExecuteAsync_WithTimeoutException_ShouldRetry()
    {
        // Arrange
        int attemptCount = 0;
        var operation = new Func<Task<string>>(async () =>
        {
            attemptCount++;
            if (attemptCount < 2)
            {
                throw new TimeoutException("Operation timed out");
            }
            await Task.Delay(10);
            return "success";
        });

        // Act
        var result = await _resilienceService.ExecuteAsync(operation);

        // Assert
        result.Should().Be("success");
        attemptCount.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task ExecuteAsync_WithTaskCanceledException_ShouldRetry()
    {
        // Arrange
        int attemptCount = 0;
        var operation = new Func<Task<string>>(async () =>
        {
            attemptCount++;
            if (attemptCount < 2)
            {
                throw new TaskCanceledException("Task was canceled");
            }
            await Task.Delay(10);
            return "success";
        });

        // Act
        var result = await _resilienceService.ExecuteAsync(operation);

        // Assert
        result.Should().Be("success");
        attemptCount.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task ExecuteAsync_WithPermanentFailure_ShouldThrow()
    {
        // Arrange
        var operation = new Func<Task<string>>(async () =>
        {
            await Task.Delay(10);
            throw new InvalidOperationException("Permanent failure");
        });

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _resilienceService.ExecuteAsync(operation));
    }

    [Fact]
    public async Task GetStatus_ShouldReturnCircuitBreakerStatus()
    {
        // Act
        var status = _resilienceService.GetStatus();

        // Assert
        status.Should().NotBeNullOrEmpty();
    }
}

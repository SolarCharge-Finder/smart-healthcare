using AIService.Services;
using Xunit;
using FluentAssertions;

namespace AIService.Tests.Services;

public class NoOpCachingServiceTests
{
    private readonly NoOpCachingService _service;

    public NoOpCachingServiceTests()
    {
        _service = new NoOpCachingService();
    }

    [Fact]
    public async Task GetAsync_ShouldAlwaysReturnNull()
    {
        // Act
        var result = await _service.GetAsync<string>("any-key");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_ShouldNotThrow()
    {
        // Act & Assert
        await _service.SetAsync("key", "value");
    }

    [Fact]
    public async Task RemoveAsync_ShouldNotThrow()
    {
        // Act & Assert
        await _service.RemoveAsync("key");
    }

    [Fact]
    public async Task ExistsAsync_ShouldAlwaysReturnFalse()
    {
        // Act
        var result = await _service.ExistsAsync("key");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task FlushAsync_ShouldNotThrow()
    {
        // Act & Assert
        await _service.FlushAsync();
    }
}

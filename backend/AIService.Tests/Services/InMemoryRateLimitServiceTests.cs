using AIService.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AIService.Tests.Services;

public class InMemoryRateLimitServiceTests
{
    private readonly InMemoryRateLimitService _service;

    public InMemoryRateLimitServiceTests()
    {
        var logger = new Mock<ILogger<InMemoryRateLimitService>>();
        _service = new InMemoryRateLimitService(logger.Object);
    }

    [Fact]
    public async Task CheckRateLimitAsync_ShouldBlockAfterMaxRequests()
    {
        const string user = "rate-limit-test-user";

        for (var i = 0; i < 5; i++)
        {
            var result = await _service.CheckRateLimitAsync(user, maxRequests: 5, windowSeconds: 3600);
            result.allowed.Should().BeTrue();
        }

        var blocked = await _service.CheckRateLimitAsync(user, maxRequests: 5, windowSeconds: 3600);
        blocked.allowed.Should().BeFalse();
        blocked.remaining.Should().Be(0);
    }

    [Fact]
    public async Task ResetUserLimitAsync_ShouldAllowRequestsAgain()
    {
        const string user = "rate-limit-reset-user";

        for (var i = 0; i < 6; i++)
        {
            await _service.CheckRateLimitAsync(user, maxRequests: 5, windowSeconds: 3600);
        }

        await _service.ResetUserLimitAsync(user);

        var result = await _service.CheckRateLimitAsync(user, maxRequests: 5, windowSeconds: 3600);
        result.allowed.Should().BeTrue();
    }
}

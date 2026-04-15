using AIService.Services;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Microsoft.Extensions.Logging;

namespace AIService.Tests.Services;

public class SecretsServiceTests : IDisposable
{
    private readonly string _testApiKey = "sk-proj-test1234567890abcdefghijklmnopqrstuvwxyz";
    private readonly Mock<ILogger<SecretsService>> _mockLogger;

    public SecretsServiceTests()
    {
        _mockLogger = new Mock<ILogger<SecretsService>>();
    }

    public void Dispose()
    {
        // Clean up environment variables
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
    }

    [Fact]
    public void GetOpenAIApiKey_WhenEnvVarSet_ShouldReturnEnvVar()
    {
        // Arrange
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", _testApiKey);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "OpenAI:ApiKey", "sk-proj-config1234567890abcdefghijklmnopqr" }
            })
            .Build();

        var service = new SecretsService(config, _mockLogger.Object);

        // Act
        var result = service.GetOpenAIApiKey();

        // Assert
        result.Should().Be(_testApiKey);
    }

    [Fact]
    public void GetOpenAIApiKey_WhenEnvVarEmpty_ShouldReturnConfigValue()
    {
        // Arrange
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "OpenAI:ApiKey", "sk-proj-config1234567890abcdefghijklmnopqr" }
            })
            .Build();

        var service = new SecretsService(config, _mockLogger.Object);

        // Act
        var result = service.GetOpenAIApiKey();

        // Assert
        result.Should().Be("sk-proj-config1234567890abcdefghijklmnopqr");
    }

    [Fact]
    public void GetOpenAIApiKey_WhenBothMissing_ShouldThrow()
    {
        // Arrange
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new SecretsService(config, _mockLogger.Object));
    }

    [Fact]
    public void GetOpenAIApiKey_EnvVarHasPriority_ShouldIgnoreConfig()
    {
        // Arrange
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", "sk-proj-env1234567890abcdefghijklmnopqrstuvwxy");
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "OpenAI:ApiKey", "sk-proj-config1234567890abcdefghijklmnopqr" }
            })
            .Build();

        var service = new SecretsService(config, _mockLogger.Object);

        // Act
        var result = service.GetOpenAIApiKey();

        // Assert
        result.Should().Be("sk-proj-env1234567890abcdefghijklmnopqrstuvwxy");
    }

    [Fact]
    public void Constructor_WhenSecretsConfigured_ShouldNotThrow()
    {
        // Arrange
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", _testApiKey);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act & Assert
        var exception = Record.Exception(() => new SecretsService(config, _mockLogger.Object));
        exception.Should().BeNull();
    }

    [Fact]
    public void Constructor_WhenSecretsNotConfigured_ShouldThrow()
    {
        // Arrange
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new SecretsService(config, _mockLogger.Object));
    }

    [Fact]
    public void ValidateSecretsConfigured_WhenValid_ShouldNotThrow()
    {
        // Arrange
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", _testApiKey);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var service = new SecretsService(config, _mockLogger.Object);

        // Act & Assert
        var exception = Record.Exception(() => service.ValidateSecretsConfigured());
        exception.Should().BeNull();
    }
}

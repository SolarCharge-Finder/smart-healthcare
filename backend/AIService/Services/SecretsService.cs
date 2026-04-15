namespace AIService.Services;

/// <summary>
/// Manages secure access to sensitive configuration (API keys, database passwords, etc).
/// Loads from environment variables and securely encrypted config files.
/// Never stores secrets in code or committed files.
/// </summary>
public interface ISecretsService
{
    /// <summary>Gets the OpenAI API key from secure configuration.</summary>
    string GetOpenAIApiKey();

    /// <summary>Gets the OpenAI model name from configuration.</summary>
    string GetOpenAIModel();

    /// <summary>Validates that all required secrets are configured.</summary>
    void ValidateSecretsConfigured();
}

public class SecretsService : ISecretsService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SecretsService> _logger;

    public SecretsService(IConfiguration configuration, ILogger<SecretsService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // Validate on startup
        ValidateSecretsConfigured();
    }

    /// <summary>
    /// Retrieves OpenAI API key from environment variables (highest priority) or appsettings.
    /// Priority: Environment variable > User Secrets > appsettings.json > appsettings.Development.json
    /// </summary>
    public string GetOpenAIApiKey()
    {
        // Try environment variable first (highest priority for production)
        var envKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
            ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (!string.IsNullOrEmpty(envKey))
        {
            _logger.LogInformation("Using OpenAI API key from environment variable");
            return envKey;
        }

        // Fall back to configuration (appsettings / user secrets)
        var configKey = _configuration["OpenAI:ApiKey"];
        if (!string.IsNullOrEmpty(configKey))
        {
            _logger.LogInformation("Using OpenAI API key from configuration");
            return configKey;
        }

        throw new InvalidOperationException(
            "OpenAI API key not found. Set OPENAI_API_KEY environment variable or configure in appsettings.json"
        );
    }

    /// <summary>Gets the OpenAI model to use for API calls.</summary>
    public string GetOpenAIModel()
    {
        var model = _configuration["OpenAI:Model"] ?? "gpt-4o";
        _logger.LogInformation("Using OpenAI model: {Model}", model);
        return model;
    }

    /// <summary>Validates that all required secrets are properly configured.</summary>
    public void ValidateSecretsConfigured()
    {
        var errors = new List<string>();

        // Check OpenAI API key
        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
            ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            ?? _configuration["OpenAI:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            errors.Add("AI API key is not configured. Set GEMINI_API_KEY (preferred) or OPENAI_API_KEY environment variable.");
        }
        else if (apiKey.Length < 20)
        {
            errors.Add("OpenAI API key appears invalid (too short).");
        }

        if (errors.Any())
        {
            _logger.LogError("Secrets validation failed: {Errors}", string.Join(" | ", errors));
            throw new InvalidOperationException(
                $"Secrets configuration incomplete:\n{string.Join("\n", errors)}"
            );
        }

        _logger.LogInformation("✓ All required secrets validated successfully");
    }
}

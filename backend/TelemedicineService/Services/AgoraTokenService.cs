using AgoraIO.Media;
using Microsoft.Extensions.Options;

namespace TelemedicineService.Services;

/// <summary>
/// Implementation of Agora RTC token generation via official Agora dynamic key builder.
/// </summary>
public class AgoraTokenService : IAgoraTokenService
{
    private readonly AgoraOptions _options;
    private readonly ILogger<AgoraTokenService> _logger;

    public AgoraTokenService(
        IOptions<AgoraOptions> options,
        ILogger<AgoraTokenService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public string GenerateToken(string channelName, uint expirySeconds = 3600)
    {
        if (string.IsNullOrWhiteSpace(_options.AppId))
            throw new InvalidOperationException("Agora AppId is not configured");

        if (string.IsNullOrWhiteSpace(_options.AppCertificate))
            throw new InvalidOperationException("Agora AppCertificate is not configured");

        try
        {
            var timestamp = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var privilegeExpiredTs = timestamp + expirySeconds;

            // uid 0 allows the client to join without binding token to a specific uid.
            var token = RtcTokenBuilder.buildTokenWithUID(
                _options.AppId,
                _options.AppCertificate,
                channelName,
                0,
                RtcTokenBuilder.Role.RolePublisher,
                privilegeExpiredTs);

            _logger.LogInformation(
                "Generated Agora token for channel: {ChannelName}, expires in {ExpirySeconds}s",
                channelName, expirySeconds);

            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate Agora token for channel: {ChannelName}", channelName);
            throw;
        }
    }
}

/// <summary>
/// Configuration options for Agora integration.
/// </summary>
public class AgoraOptions
{
    public const string SectionName = "Agora";

    /// <summary>
    /// Agora Application ID (also called App ID).
    /// This is public and safe to use server-side.
    /// Should NEVER be exposed to frontend in a way that allows token generation.
    /// </summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    /// Agora App Certificate (private key for signing tokens).
    /// MUST be kept secret and never exposed to frontend.
    /// Used only server-side for token generation.
    /// </summary>
    public string AppCertificate { get; set; } = string.Empty;

    /// <summary>
    /// Default token expiry time in seconds.
    /// Can be overridden per token generation.
    /// Recommended: 1 hour (3600 seconds) for short-lived sessions.
    /// </summary>
    public uint TokenExpirySeconds { get; set; } = 3600;
}

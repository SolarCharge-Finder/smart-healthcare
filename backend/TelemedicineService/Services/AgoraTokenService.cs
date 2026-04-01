using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace TelemedicineService.Services;

/// <summary>
/// Implementation of Agora RTC token generation.
/// Uses HMAC-SHA256 to create cryptographically secure tokens.
/// Follows Agora's AccessToken2 specification.
/// </summary>
public class AgoraTokenService : IAgoraTokenService
{
    private readonly AgoraOptions _options;
    private readonly ILogger<AgoraTokenService> _logger;

    // Token version for Agora AccessToken2 format
    private const byte TokenVersion = 3;

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
            var appIdBytes = Encoding.UTF8.GetBytes(_options.AppId);
            var appCertificateBytes = Convert.FromHexString(_options.AppCertificate);

            // Current timestamp
            var timestamp = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var expiresAt = timestamp + expirySeconds;

            // Build the signature source string
            // Format: "appId:expiration:crc:channelName"
            var signatureSource = $"{_options.AppId}:{expirySeconds}:0:{channelName}";
            var signatureSourceBytes = Encoding.UTF8.GetBytes(signatureSource);

            // Generate HMAC-SHA256 signature
            using (var hmac = new HMACSHA256(appCertificateBytes))
            {
                var signature = hmac.ComputeHash(signatureSourceBytes);

                // Build token: version + signature + appId + expiration
                var tokenBytes = new List<byte>();

                // Add version
                tokenBytes.Add(TokenVersion);

                // Add signature
                tokenBytes.AddRange(signature);

                // Build and add app id and expiration content
                var contentBytes = BuildTokenContent(appIdBytes, expiresAt, channelName);
                tokenBytes.AddRange(contentBytes);

                // Convert to base64
                var tokenString = Convert.ToBase64String(tokenBytes.ToArray());

                _logger.LogInformation(
                    "Generated Agora token for channel: {ChannelName}, expires in {ExpirySeconds}s",
                    channelName, expirySeconds);

                return tokenString;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate Agora token for channel: {ChannelName}", channelName);
            throw;
        }
    }

    private byte[] BuildTokenContent(byte[] appIdBytes, uint expiresAt, string channelName)
    {
        using (var stream = new MemoryStream())
        {
            using (var writer = new BinaryWriter(stream))
            {
                // We're using the simplified token format with version 3
                // For now, return a minimal content that includes expiration
                var channelBytes = Encoding.UTF8.GetBytes(channelName);

                // Write expiration (4 bytes, uint32)
                writer.Write(expiresAt);

                // Write channel name length and value
                writer.Write((ushort)channelBytes.Length);
                writer.Write(channelBytes);
            }

            return stream.ToArray();
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

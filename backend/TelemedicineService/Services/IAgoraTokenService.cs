namespace TelemedicineService.Services;

/// <summary>
/// Service for generating Agora RTC tokens.
/// Handles HMAC-SHA256 token generation with proper expiry and channel binding.
/// </summary>
public interface IAgoraTokenService
{
    /// <summary>
    /// Generates an Agora RTC token for a given channel.
    /// Tokens are publisher tokens (both send and receive permission).
    /// </summary>
    /// <param name="channelName">Agora channel name (e.g., "appointment-{id}")</param>
    /// <param name="expirySeconds">Token expiry time in seconds from now</param>
    /// <returns>Agora RTC access token</returns>
    string GenerateToken(string channelName, uint expirySeconds = 3600);
}

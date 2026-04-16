namespace Shared.Contracts.Infrastructure.Auth;

using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using Microsoft.Extensions.Configuration;

using Shared.Contracts.DTOs;
using Shared.Contracts.Enums;

public class AuthServiceClient : IAuthServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly string _serviceName;
    private readonly string _apiKey;

    public AuthServiceClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;

        // Read from config (appsettings or env)
        _serviceName = configuration["ServiceName"]
            ?? throw new ArgumentNullException("ServiceName is not configured");

        _apiKey = configuration["InternalApiKey"]
            ?? throw new ArgumentNullException("InternalApiKey is not configured");
    }

    public async Task GrantRoleAsync(Guid userId, UserRole role)
    {
        // =========================
        // STEP 1: Get internal token
        // =========================

        var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "/internal/auth/token")
        {
            Content = JsonContent.Create(new InternalTokenRequest
            {
                ServiceName = _serviceName
            })
        };

        tokenRequest.Headers.Add("x-api-key", _apiKey);

        var tokenResponse = await _httpClient.SendAsync(tokenRequest);
        var tokenContent = await tokenResponse.Content.ReadAsStringAsync();

        Console.WriteLine($"[AuthClient] Token response: {tokenResponse.StatusCode} - {tokenContent}");

        if (!tokenResponse.IsSuccessStatusCode)
        {
            throw new Exception(
                $"Token request failed: {tokenResponse.StatusCode} - {tokenContent}"
            );
        }

        var token = (await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>())?.Token;

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new Exception("Internal token is null or empty");
        }

        // =========================
        // STEP 2: Grant role
        // =========================

        var roleRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/users/internal/{userId}/role"
        )
        {
            Content = JsonContent.Create(new
            {
                role = role.ToString() // IMPORTANT FIX
            })
        };

        roleRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(roleRequest);
        var roleContent = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"[AuthClient] Role response: {response.StatusCode} - {roleContent}");

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(
                $"Role update failed: {response.StatusCode} - {roleContent}"
            );
        }
    }
}

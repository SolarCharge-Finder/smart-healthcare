namespace Doctor.Infrastructure.Services;

using System.Net.Http.Json;

using Doctor.Application.DTOs;
using Doctor.Application.Interfaces;

using Shared.Contracts.Enums;

public class AuthServiceClient : IAuthServiceClient
{
    private readonly HttpClient _httpClient;

    public AuthServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task GrantRoleAsync(Guid userId, UserRole role)
    {
        // get internal token
        var tokenResponse = await _httpClient.PostAsync("/internal/auth/token", null);

        if (!tokenResponse.IsSuccessStatusCode)
        {
            throw new Exception("Failed to get internal token from AuthService");
        }

        var token = (await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>())?.Token;

        if (string.IsNullOrEmpty(token))
        {
            throw new Exception("Internal token is null");
        }

        // create request with token
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/users/internal/{userId}/role"
        )
        {
            Content = JsonContent.Create(new { role })
        };

        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // send request
        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Failed to update user role in AuthService");
        }
    }
}

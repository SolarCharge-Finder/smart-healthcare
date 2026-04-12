namespace Doctor.Infrastructure.Services;

using System.Net.Http.Json;
using Doctor.Application.Interfaces;

public class AuthServiceClient : IAuthServiceClient
{
    private readonly HttpClient _httpClient;

    public AuthServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task GrantRoleAsync(Guid userId, string role)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"/users/{userId}/role",
            new { role }
        );

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Failed to update user role in AuthService");
        }
    }
}
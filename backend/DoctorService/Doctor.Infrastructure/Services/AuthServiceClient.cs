namespace Doctor.Infrastructure.Services;

using System.Net.Http.Json;

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

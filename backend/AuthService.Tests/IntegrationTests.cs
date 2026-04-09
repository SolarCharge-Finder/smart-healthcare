using Xunit;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Auth.Application.DTOs;
using AuthService.Tests.Fixtures;
using AuthService.Tests.Helpers;

public class IntegrationTests : IClassFixture<PostgresFixture>
{
private readonly HttpClient _client;

public IntegrationTests(PostgresFixture fixture)
{
    var factory = new TestingFactory(fixture.Db);
    _client = factory.CreateClient();
}

[Fact]
public async Task Full_User_Lifecycle_Should_Work()
{
    var email = "integration@test.com";
    var password = "123456";

    // REGISTER
    await _client.PostAsJsonAsync("/auth/register", new
    {
        name = "Integration User",
        email,
        password,
        role = "Undefined"
    });

    // VERIFY
    var verifyToken = FakeEmailService.LastVerificationToken;
    await _client.PostAsJsonAsync("/auth/verify", new { token = verifyToken });

    // LOGIN
    var login = await _client.PostAsJsonAsync("/auth/login", new
    {
        email,
        password
    });

    var data = await login.Content.ReadFromJsonAsync<LoginResponse>();

    _client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", data!.Token);

    // UPDATE PROFILE
    await _client.PutAsJsonAsync("/users/profile", new
    {
        name = "Updated Integration User"
    });

    // CHANGE PASSWORD
    await _client.PutAsJsonAsync("/users/change-password", new
    {
        currentPassword = password,
        newPassword = "newpass123"
    });

    // FORGOT PASSWORD
    await _client.PostAsJsonAsync("/auth/forgot-password", new { email });

    var resetToken = FakeEmailService.LastPasswordResetToken;

    // RESET PASSWORD
    await _client.PostAsJsonAsync("/auth/reset-password", new
    {
        token = resetToken,
        newPassword = "resetpass123"
    });
}

}

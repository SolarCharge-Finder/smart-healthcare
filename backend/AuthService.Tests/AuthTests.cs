using System.Net;
using System.Net.Http.Json;

using Auth.Application.DTOs;
using Auth.Infrastructure.Data;
using Auth.Domain.Entities;
using Auth.Application.Interfaces;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Xunit;
using Shared.Contracts.Enums;

using AuthService.Tests.Fakes;

namespace AuthService.Tests;

public class AuthTests : IClassFixture<TestingFactory>
{
    private readonly HttpClient _client;
    private readonly TestingFactory _factory;

    public AuthTests(TestingFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        factory.ResetDatabaseAsync().GetAwaiter().GetResult();
    }

    // helper (same pattern as Admin)
    private void SetUser(string userId, string role = "User")
    {
        _client.DefaultRequestHeaders.Remove("x-user-id");
        _client.DefaultRequestHeaders.Add("x-user-id", userId);

        _client.DefaultRequestHeaders.Remove("x-user-role");
        _client.DefaultRequestHeaders.Add("x-user-role", role);
    }

    // =========================
    // AUTH CONTROLLER TESTS
    // =========================

    [Fact]
    public async Task Register_Should_Create_User()
    {
        var request = new RegisterRequest
        {
            Email = "test@test.com",
            Password = "Password123!",
            Name = "Test User"
        };

        var response = await _client.PostAsJsonAsync("/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        db.PendingUsers.Should().Contain(u => u.Email == "test@test.com");
    }

    [Fact]
    public async Task Verify_Should_Move_User_From_Pending_To_Users()
    {
        var request = new RegisterRequest
        {
            Email = "verify@test.com",
            Password = "Password123!",
            Name = "Verify User"
        };

        await _client.PostAsJsonAsync("/auth/register", request);

        string token;

        using (var scope = _factory.Services.CreateScope())
        {
            var fakeEmail = scope.ServiceProvider
                .GetRequiredService<IEmailService>() as FakeEmailService;

            token = fakeEmail!.VerificationEmails
                .First(x => x.Email == "verify@test.com").Token;
        }

        var verifyResponse = await _client.PostAsJsonAsync("/auth/verify",
            new VerifyRequest { Token = token });

        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

            db.Users.Should().Contain(u => u.Email == "verify@test.com");
            db.PendingUsers.Should().NotContain(u => u.Email == "verify@test.com");
        }
    }

    [Fact]
    public async Task Register_Should_Fail_If_Email_Exists()
    {
        var request = new RegisterRequest
        {
            Email = "dup@test.com",
            Password = "Password123!",
            Name = "User"
        };

        await _client.PostAsJsonAsync("/auth/register", request);

        var response = await _client.PostAsJsonAsync("/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_Should_Return_Token()
    {
        var register = new RegisterRequest
        {
            Email = "login@test.com",
            Password = "Password123!",
            Name = "Login User"
        };

        await _client.PostAsJsonAsync("/auth/register", register);

        using (var scope = _factory.Services.CreateScope())
        {
            var fakeEmail = scope.ServiceProvider
                .GetRequiredService<IEmailService>() as FakeEmailService;

            var token = fakeEmail!.VerificationEmails
                .First(x => x.Email == "login@test.com").Token;

            await _client.PostAsJsonAsync("/auth/verify",
                new VerifyRequest { Token = token });
        }

        var login = new LoginRequest
        {
            Email = "login@test.com",
            Password = "Password123!"
        };

        var response = await _client.PostAsJsonAsync("/auth/login", login);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_Should_Return_Unauthorized_For_Invalid_Credentials()
    {
        var login = new LoginRequest
        {
            Email = "wrong@test.com",
            Password = "wrong"
        };

        var response = await _client.PostAsJsonAsync("/auth/login", login);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =========================
    // USERS CONTROLLER TESTS
    // =========================

    [Fact]
    public async Task GetCurrentUser_Should_Return_User()
    {
        var userId = Guid.NewGuid();

        // seed user
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

            db.Users.Add(new User
            {
                Id = userId,
                Email = "me@test.com",
                Name = "Me",
                PasswordHash = "hash"
            });

            await db.SaveChangesAsync();
        }

        SetUser(userId.ToString(), "User");

        var response = await _client.GetAsync("/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetCurrentUser_Should_Return_Unauthorized_If_No_User()
    {
        var response = await _client.GetAsync("/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateProfile_Should_Update_Name()
    {
        var userId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

            db.Users.Add(new User
            {
                Id = userId,
                Email = "update@test.com",
                Name = "Old Name",
                PasswordHash = "hash"
            });

            await db.SaveChangesAsync();
        }

        SetUser(userId.ToString(), "User");

        var response = await _client.PutAsJsonAsync("/users/profile",
            new UpdateProfileRequest { Name = "New Name" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var verifyScope = _factory.Services.CreateScope();
        var dbVerify = verifyScope.ServiceProvider.GetRequiredService<AuthDbContext>();

        dbVerify.Users.First(u => u.Id == userId).Name.Should().Be("New Name");
    }

    [Fact]
    public async Task ChangePassword_Should_Work()
    {
        var userId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

            db.Users.Add(new User
            {
                Id = userId,
                Email = "pass@test.com",
                Name = "User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("oldpass")
            });

            await db.SaveChangesAsync();
        }

        SetUser(userId.ToString(), "User");

        var response = await _client.PutAsJsonAsync("/users/change-password",
            new ChangePasswordRequest
            {
                CurrentPassword = "oldpass",
                NewPassword = "newpass"
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SetRole_Should_Require_Admin()
    {
        var userId = Guid.NewGuid();

        // seed target user
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

            db.Users.Add(new User
            {
                Id = userId,
                Email = "role@test.com",
                Name = "User",
                PasswordHash = "hash"
            });

            await db.SaveChangesAsync();
        }

        // NOT admin
        SetUser(Guid.NewGuid().ToString(), "User");

        var response = await _client.PostAsJsonAsync($"/users/{userId}/role",
            new SetRole { Role = UserRole.Admin });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // =========================
    // INTERNAL AUTH TESTS
    // =========================

    [Fact]
    public async Task InternalToken_Should_Return_Token_With_Valid_Key()
    {
        var request = new InternalTokenRequest
        {
            ServiceName = "admin-service"
        };

        var message = new HttpRequestMessage(HttpMethod.Post, "/internal/auth/token");
        message.Headers.Add("x-api-key", "test-api-key");
        message.Content = JsonContent.Create(request);

        var response = await _client.SendAsync(message);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task InternalToken_Should_Return_Unauthorized_For_Invalid_Key()
    {
        var request = new InternalTokenRequest
        {
            ServiceName = "admin-service"
        };

        var message = new HttpRequestMessage(HttpMethod.Post, "/internal/auth/token");
        message.Headers.Add("x-api-key", "wrong-key");
        message.Content = JsonContent.Create(request);

        var response = await _client.SendAsync(message);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task InternalToken_Should_Fail_For_Invalid_Service()
    {
        var request = new InternalTokenRequest
        {
            ServiceName = "hacker-service"
        };

        var message = new HttpRequestMessage(HttpMethod.Post, "/internal/auth/token");
        message.Headers.Add("x-api-key", "test-api-key");
        message.Content = JsonContent.Create(request);

        var response = await _client.SendAsync(message);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
using System.Net;
using System.Net.Http.Json;

using Auth.Application.DTOs;
using Auth.Application.Interfaces;
using Auth.Infrastructure.Data;
using Auth.Domain.Entities;

using AuthService.Tests.Fakes;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Testcontainers.PostgreSql;

using Xunit;

namespace AuthService.Tests;

public class AuthPostgresIntegrationTests : IAsyncLifetime
{
    private PostgreSqlContainer _db = null!;
    private HttpClient _client = null!;
    private PostgreSqlTestingFactory _factory = null!;

    private void SetUser(string userId, string role = "User")
    {
        _client.DefaultRequestHeaders.Remove("x-user-id");
        _client.DefaultRequestHeaders.Add("x-user-id", userId);

        _client.DefaultRequestHeaders.Remove("x-user-role");
        _client.DefaultRequestHeaders.Add("x-user-role", role);
    }

    public async Task InitializeAsync()
    {
        _db = new PostgreSqlBuilder("postgres:15")
            .WithDatabase("authdb")
            .WithUsername("test")
            .WithPassword("test")
            .Build();

        await _db.StartAsync();

        _factory = new PostgreSqlTestingFactory(_db);
        _client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        db.Database.Migrate();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        await _db.DisposeAsync();
    }

    [Fact]
    public async Task Register_Should_Persist_PendingUser_In_Postgres()
    {
        await _client.PostAsJsonAsync("/auth/register", new RegisterRequest
        {
            Email = "pg@test.com",
            Password = "Password123!",
            Name = "PG User"
        });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        var pending = await db.PendingUsers.ToListAsync();

        pending.Should().Contain(u => u.Email == "pg@test.com");
    }

    [Fact]
    public async Task Verify_Should_Move_User_To_Users_Table_In_Postgres()
    {
        await _client.PostAsJsonAsync("/auth/register", new RegisterRequest
        {
            Email = "verifypg@test.com",
            Password = "Password123!",
            Name = "Verify PG"
        });

        string token;

        using (var scope = _factory.Services.CreateScope())
        {
            var fakeEmail = scope.ServiceProvider
                .GetRequiredService<IEmailService>() as FakeEmailService;

            token = fakeEmail!.VerificationEmails
                .First(x => x.Email == "verifypg@test.com").Token;
        }

        var response = await _client.PostAsJsonAsync("/auth/verify",
            new VerifyRequest { Token = token });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

            var users = await db.Users.ToListAsync();
            var pending = await db.PendingUsers.ToListAsync();

            users.Should().Contain(u => u.Email == "verifypg@test.com");
            pending.Should().NotContain(u => u.Email == "verifypg@test.com");
        }
    }

    [Fact]
    public async Task Login_Should_Return_Token_After_Verification_In_Postgres()
    {
        await _client.PostAsJsonAsync("/auth/register", new RegisterRequest
        {
            Email = "loginpg@test.com",
            Password = "Password123!",
            Name = "Login PG"
        });

        string token;

        using (var scope = _factory.Services.CreateScope())
        {
            var fakeEmail = scope.ServiceProvider
                .GetRequiredService<IEmailService>() as FakeEmailService;

            token = fakeEmail!.VerificationEmails
                .First(x => x.Email == "loginpg@test.com").Token;
        }

        await _client.PostAsJsonAsync("/auth/verify",
            new VerifyRequest { Token = token });

        var response = await _client.PostAsJsonAsync("/auth/login", new LoginRequest
        {
            Email = "loginpg@test.com",
            Password = "Password123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_Should_Fail_Without_Verification_In_Postgres()
    {
        await _client.PostAsJsonAsync("/auth/register", new RegisterRequest
        {
            Email = "nologinpg@test.com",
            Password = "Password123!",
            Name = "No Verify"
        });

        var response = await _client.PostAsJsonAsync("/auth/login", new LoginRequest
        {
            Email = "nologinpg@test.com",
            Password = "Password123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProfile_Should_Persist_Changes_In_Postgres()
    {
        var userId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

            db.Users.Add(new User
            {
                Id = userId,
                Email = "profilepg@test.com",
                Name = "Old",
                PasswordHash = "hash"
            });

            await db.SaveChangesAsync();
        }

        SetUser(userId.ToString(), "User");

        var response = await _client.PutAsJsonAsync("/users/profile",
            new UpdateProfileRequest { Name = "New" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var verifyScope = _factory.Services.CreateScope();
        var dbVerify = verifyScope.ServiceProvider.GetRequiredService<AuthDbContext>();

        var updated = await dbVerify.Users.FirstAsync(u => u.Id == userId);

        updated.Name.Should().Be("New");
    }

    [Fact]
    public async Task ChangePassword_Should_Update_Hash_In_Postgres()
    {
        var userId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

            db.Users.Add(new User
            {
                Id = userId,
                Email = "passpg@test.com",
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

        using var scope2 = _factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<AuthDbContext>();

        var user = await db2.Users.FirstAsync(u => u.Id == userId);

        BCrypt.Net.BCrypt.Verify("newpass", user.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task SetRole_Should_Update_UserRole_In_Postgres()
    {
        var userId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

            db.Users.Add(new User
            {
                Id = userId,
                Email = "rolepg@test.com",
                Name = "User",
                PasswordHash = "hash"
            });

            await db.SaveChangesAsync();
        }

        SetUser(Guid.NewGuid().ToString(), "Admin");

        var response = await _client.PostAsJsonAsync($"/users/{userId}/role",
            new SetRole { Role = Shared.Contracts.Enums.UserRole.Admin });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scopeVerify = _factory.Services.CreateScope();
        var dbVerify = scopeVerify.ServiceProvider.GetRequiredService<AuthDbContext>();

        var user = await dbVerify.Users.FirstAsync(u => u.Id == userId);

        user.Role.Should().Be(Shared.Contracts.Enums.UserRole.Admin);
    }

    [Fact]
    public async Task ForgotPassword_Should_Send_Reset_Email()
    {
        await _client.PostAsJsonAsync("/auth/register", new RegisterRequest
        {
            Email = "forgot@test.com",
            Password = "Password123!",
            Name = "User"
        });

        // verify user first
        string token;

        using (var scope = _factory.Services.CreateScope())
        {
            var fakeEmail = scope.ServiceProvider
                .GetRequiredService<IEmailService>() as FakeEmailService;

            token = fakeEmail!.VerificationEmails
                .First(x => x.Email == "forgot@test.com").Token;
        }

        await _client.PostAsJsonAsync("/auth/verify",
            new VerifyRequest { Token = token });

        // now call forgot password
        await _client.PostAsJsonAsync("/auth/forgot-password",
            new ForgotPasswordRequest { Email = "forgot@test.com" });

        using (var scope = _factory.Services.CreateScope())
        {
            var fakeEmail = scope.ServiceProvider
                .GetRequiredService<IEmailService>() as FakeEmailService;

            fakeEmail!.PasswordResetEmails
                .Should().Contain(x => x.Email == "forgot@test.com");
        }
    }

    [Fact]
    public async Task ResetPassword_Should_Update_Password()
    {
        await _client.PostAsJsonAsync("/auth/register", new RegisterRequest
        {
            Email = "reset@test.com",
            Password = "oldpass",
            Name = "User"
        });

        string verifyToken;

        using (var scope = _factory.Services.CreateScope())
        {
            var fakeEmail = scope.ServiceProvider
                .GetRequiredService<IEmailService>() as FakeEmailService;

            verifyToken = fakeEmail!.VerificationEmails
                .First(x => x.Email == "reset@test.com").Token;
        }

        await _client.PostAsJsonAsync("/auth/verify",
            new VerifyRequest { Token = verifyToken });

        // now trigger forgot password
        await _client.PostAsJsonAsync("/auth/forgot-password",
            new ForgotPasswordRequest { Email = "reset@test.com" });

        string resetToken;

        using (var scope = _factory.Services.CreateScope())
        {
            var fakeEmail = scope.ServiceProvider
                .GetRequiredService<IEmailService>() as FakeEmailService;

            resetToken = fakeEmail!.PasswordResetEmails
                .First(x => x.Email == "reset@test.com").Token;
        }

        var response = await _client.PostAsJsonAsync("/auth/reset-password",
            new ResetPasswordRequest
            {
                Token = resetToken,
                NewPassword = "newpass"
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SetRole_Should_Work_For_Admin_In_Postgres()
    {
        var userId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

            db.Users.Add(new User
            {
                Id = userId,
                Email = "adminrole@test.com",
                Name = "User",
                PasswordHash = "hash"
            });

            await db.SaveChangesAsync();
        }

        SetUser(Guid.NewGuid().ToString(), "Admin");

        var response = await _client.PostAsJsonAsync($"/users/{userId}/role",
            new SetRole { Role = Shared.Contracts.Enums.UserRole.Admin });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;
using UserService.Application.DTOs;

namespace UserService.Tests;

public class UserTests : IClassFixture<TestingFactory>
{
    private readonly HttpClient _client;

    public UserTests(TestingFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateUser_Should_Work()
    {
        var request = new CreateUserRequest
        {
            FullName = "Test User",
            Email = "test@test.com"
        };

        var response = await _client.PostAsJsonAsync("/users", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMe_Should_Return_User()
    {
        await _client.PostAsJsonAsync("/users", new CreateUserRequest
        {
            FullName = "Me",
            Email = "me@test.com"
        });

        var response = await _client.GetAsync("/users/me");

        response.EnsureSuccessStatusCode();

        var user = await response.Content.ReadFromJsonAsync<UserResponse>();

        user.Should().NotBeNull();
        user!.FullName.Should().Be("Me");
    }

    [Fact]
    public async Task UpdateUser_Should_Work()
    {
        await _client.PostAsJsonAsync("/users", new CreateUserRequest
        {
            FullName = "Old",
            Email = "old@test.com"
        });

        var response = await _client.PutAsJsonAsync("/users", new UpdateUserRequest
        {
            FullName = "Updated"
        });

        response.EnsureSuccessStatusCode();

        var updated = await _client.GetFromJsonAsync<UserResponse>("/users/me");

        updated!.FullName.Should().Be("Updated");
    }

    [Fact]
    public async Task DeactivateUser_Should_Work()
    {
        await _client.PostAsJsonAsync("/users", new CreateUserRequest
        {
            FullName = "Deactivate",
            Email = "d@test.com"
        });

        var response = await _client.PatchAsync("/users/me/deactivate", null);

        response.EnsureSuccessStatusCode();

        var user = await _client.GetAsync("/users/me");

        user.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAll_Should_Return_Users()
    {
        await _client.PostAsJsonAsync("/users", new CreateUserRequest
        {
            FullName = "Admin User",
            Email = "admin@test.com"
        });

        var response = await _client.GetAsync("/users");

        response.EnsureSuccessStatusCode();

        var users = await response.Content.ReadFromJsonAsync<List<UserResponse>>();

        users.Should().NotBeNull();
        users!.Count.Should().BeGreaterThan(0);
    }
}
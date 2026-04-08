using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace AuthService.Tests;

public class AuthTests : IClassFixture<TestingFactory>
{
    private readonly HttpClient _client;

    public AuthTests(TestingFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_Should_Create_User()
    {
        var request = new
        {
            email = "test@test.com",
            password = "123456",
            role = "Patient"
        };

        var response = await _client.PostAsJsonAsync("/auth/register", request);

        var body = await response.Content.ReadAsStringAsync();
        Console.WriteLine(body);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_Should_Fail_When_User_Already_Exists()
    {
        var request = new
        {
            email = "duplicate@test.com",
            password = "123456",
            role = "Patient"
        };

        await _client.PostAsJsonAsync("/auth/register", request);
        var response = await _client.PostAsJsonAsync("/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_Should_Return_Token_For_Valid_User()
    {
        var register = new
        {
            email = "login@test.com",
            password = "123456",
            role = "Patient"
        };

        await _client.PostAsJsonAsync("/auth/register", register);

        var login = new
        {
            email = "login@test.com",
            password = "123456"
        };

        var response = await _client.PostAsJsonAsync("/auth/login", login);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();

        body.Should().ContainKey("token");
        body["token"].ToString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_Should_Fail_For_Invalid_Password()
    {
        var register = new
        {
            email = "wrongpass@test.com",
            password = "123456",
            role = "Patient"
        };

        await _client.PostAsJsonAsync("/auth/register", register);

        var login = new
        {
            email = "wrongpass@test.com",
            password = "wrongpassword"
        };

        var response = await _client.PostAsJsonAsync("/auth/login", login);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
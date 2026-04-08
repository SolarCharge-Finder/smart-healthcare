using Auth.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection.Extensions;


using Auth.Application.Interfaces;

namespace AuthService.Tests;

public class PostgresIntegrationTests : IAsyncLifetime
{
    private PostgreSqlContainer _db = null!;
    private HttpClient _client = null!;
    private TestingFactory _factory = null!;

    public async Task InitializeAsync()
    {
        _db = new PostgreSqlBuilder("postgres:15")
            .WithDatabase("authdb")
            .WithUsername("test")
            .WithPassword("test")
            .Build();

        await _db.StartAsync();

        _factory = new TestingFactory(_db);
        _client = _factory.CreateClient();

        // apply migrations
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
    public async Task Register_Should_Work_With_Postgres()
    {
        var request = new
        {
            name = "PG User",
            email = "pg@test.com",
            password = "123456",
            role = "Undefined"
        };

        var response = await _client.PostAsJsonAsync("/auth/register", request);

        var token = FakeEmailService.LastSentToken;

        await _client.PostAsJsonAsync("/auth/verify", new { token });

        response.EnsureSuccessStatusCode();
    }

    // custom factory
    public class TestingFactory : WebApplicationFactory<Program>
    {
        private readonly PostgreSqlContainer _db;

        public TestingFactory(PostgreSqlContainer db)
        {
            _db = db;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                var descriptor = services.FirstOrDefault(
                    d => d.ServiceType ==
                    typeof(DbContextOptions<AuthDbContext>));

                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<AuthDbContext>(options =>
                    options.UseNpgsql(_db.GetConnectionString()));

                services.RemoveAll<IEmailService>();
                services.AddScoped<IEmailService, FakeEmailService>();
            });
        }
    }
}
using AdminService.Infrastructure.Data;
using AdminService.Application.DTOs;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Testcontainers.PostgreSql;
using Xunit;

using System.Net.Http.Json;
using System.Linq;

namespace AdminService.Tests;

public class PostgresIntegrationTests : IAsyncLifetime
{
    private PostgreSqlContainer _db = null!;
    private HttpClient _client = null!;
    private PostgreSqlTestingFactory _factory = null!;

    public async Task InitializeAsync()
    {
        _db = new PostgreSqlBuilder("postgres:15")
            .WithDatabase("admindb")
            .WithUsername("test")
            .WithPassword("test")
            .Build();

        await _db.StartAsync();

        _factory = new PostgreSqlTestingFactory(_db);
        _client = _factory.CreateClient();

        // apply migrations
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
        db.Database.Migrate();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        await _db.DisposeAsync();
    }

    [Fact]
    public async Task CreateAdmin_Should_Work_With_Postgres()
    {
        var request = new CreateAdminRequest
        {
            FullName = "Postgres Admin"
        };

        var response = await _client.PostAsJsonAsync("/admin", request);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetAll_Should_Return_Data_From_Postgres()
    {
        await _client.PostAsJsonAsync("/admin", new CreateAdminRequest
        {
            FullName = "Admin PG"
        });

        var response = await _client.GetAsync("/admin");

        response.EnsureSuccessStatusCode();

        var admins = await response.Content.ReadFromJsonAsync<List<AdminResponse>>();

        Assert.NotNull(admins);
        Assert.NotEmpty(admins);
    }

    // Factory for PostgreSQL
    public class PostgreSqlTestingFactory : TestingFactory
    {
        private readonly PostgreSqlContainer _db;

        public PostgreSqlTestingFactory(PostgreSqlContainer db)
        {
            _db = db;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                // replace DbContext
                var descriptor = services.FirstOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AdminDbContext>));

                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<AdminDbContext>(options =>
                    options.UseNpgsql(_db.GetConnectionString()));
            });
        }
    }
}
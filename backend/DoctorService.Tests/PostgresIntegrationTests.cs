using Doctor.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;
using System.Net.Http.Json;

namespace DoctorService.Tests;

public class PostgresIntegrationTests : IAsyncLifetime
{
    private PostgreSqlContainer _db = null!;
    private HttpClient _client = null!;
    private TestingFactory _factory = null!;

    public async Task InitializeAsync()
    {
        _db = new PostgreSqlBuilder("postgres:15")
            .WithDatabase("doctordb")
            .WithUsername("test")
            .WithPassword("test")
            .Build();

        await _db.StartAsync();

        _factory = new PostgreSqlTestingFactory(_db);
        _client = _factory.CreateClient();

        // apply migrations
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DoctorDbContext>();
        db.Database.Migrate();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        await _db.DisposeAsync();
    }

    [Fact]
    public async Task CreateDoctor_Should_Work_With_Postgres()
    {
        var request = new
        {
            userId = Guid.NewGuid(),
            fullName = "Dr Postgres",
            specialization = "DB Magic",
            hospital = "Container Hospital"
        };

        var response = await _client.PostAsJsonAsync("/doctors", request);

        response.EnsureSuccessStatusCode();
    }

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
                var descriptor = services.FirstOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<DoctorDbContext>));

                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<DoctorDbContext>(options =>
                    options.UseNpgsql(_db.GetConnectionString()));
            });
        }
    }
}
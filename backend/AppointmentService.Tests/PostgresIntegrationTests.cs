using AppointmentService.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Xunit;

namespace AppointmentService.Tests;

public class PostgresIntegrationTests
    : IAsyncLifetime
{
    private PostgreSqlContainer _db = null!;
    private TestingFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _db = new PostgreSqlBuilder()
            .WithImage("postgres:15")
            .WithDatabase("appointmentsdb")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await _db.StartAsync();

        _factory = new TestingFactory(_db);
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        await _db.DisposeAsync();
    }

    [Fact]
    public async Task GetAppointments_ReturnsOk()
    {
        var response = await _client.GetAsync("/appointments");
        response.EnsureSuccessStatusCode();
    }

    public sealed class TestingFactory
        : WebApplicationFactory<Program>
    {
        private readonly PostgreSqlContainer _db;

        public TestingFactory(PostgreSqlContainer db)
        {
            _db = db;
        }

        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        _db.GetConnectionString(),
                    ["API_KEY"] = "test-key"
                });
            });

            builder.ConfigureServices(services =>
            {
                var dbDescriptor = services
                    .FirstOrDefault(d =>
                        d.ServiceType ==
                        typeof(DbContextOptions<AppointmentDbContext>));

                if (dbDescriptor is not null)
                    services.Remove(dbDescriptor);

                services.AddDbContext<AppointmentDbContext>(options =>
                    options.UseNpgsql(_db.GetConnectionString()));

                var redisDescriptor = services
                    .FirstOrDefault(d =>
                        d.ServiceType ==
                        typeof(IConnectionMultiplexer));

                if (redisDescriptor is not null)
                    services.Remove(redisDescriptor);

                services.AddSingleton<IConnectionMultiplexer>(_ =>
                    ConnectionMultiplexer.Connect(
                        "localhost:6379,abortConnect=false,connectTimeout=100"));
            });
        }
    }
}

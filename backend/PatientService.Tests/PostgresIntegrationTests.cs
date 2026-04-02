using PatientService.Infrastructure.Data;
using PatientService.Application.DTOs;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Testcontainers.PostgreSql;
using Xunit;

using System.Net.Http.Json;
using System.Linq;

namespace PatientService.Tests;

public class PostgresIntegrationTests : IAsyncLifetime
{
    private PostgreSqlContainer _db = null!;
    private HttpClient _client = null!;
    private PostgreSqlTestingFactory _factory = null!;

    public async Task InitializeAsync()
    {
        _db = new PostgreSqlBuilder("postgres:15")
            .WithDatabase("patientdb")
            .WithUsername("test")
            .WithPassword("test")
            .Build();

        await _db.StartAsync();

        _factory = new PostgreSqlTestingFactory(_db);
        _client = _factory.CreateClient();

        // apply migrations
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatientDbContext>();
        db.Database.Migrate();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        await _db.DisposeAsync();
    }

    [Fact]
    public async Task CreatePatient_Should_Work_With_Postgres()
    {
        var request = new CreatePatientRequest
        {
            FullName = "Postgres Patient",
            Email = "pg@test.com"
        };

        var response = await _client.PostAsJsonAsync("/patients", request);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetMe_Should_Return_Data_From_Postgres()
    {
        await _client.PostAsJsonAsync("/patients", new CreatePatientRequest
        {
            FullName = "Patient PG",
            Email = "userpg@test.com"
        });

        var response = await _client.GetAsync("/patients/me");

        response.EnsureSuccessStatusCode();

        var patient = await response.Content.ReadFromJsonAsync<PatientResponse>();

        Assert.NotNull(patient);
        Assert.Equal("Patient PG", patient!.FullName);
    }

    [Fact]
    public async Task DeactivatePatient_Should_Reflect_In_Postgres()
    {
        await _client.PostAsJsonAsync("/patients", new CreatePatientRequest
        {
            FullName = "Deactivate PG",
            Email = "deactivate@test.com"
        });

        await _client.PatchAsync("/patients/me/deactivate", null);

        var response = await _client.GetAsync("/patients/me");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
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
                    d => d.ServiceType == typeof(DbContextOptions<PatientDbContext>));

                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<PatientDbContext>(options =>
                    options.UseNpgsql(_db.GetConnectionString()));
            });
        }
    }
}
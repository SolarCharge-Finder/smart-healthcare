using System.Net.Http.Json;

using Doctor.Application.DTOs;
using Doctor.Infrastructure.Data;

using FluentAssertions;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Testcontainers.PostgreSql;

using Xunit;

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
        var request = new CreateDoctorRequest
        {
            FullName = "Dr Postgres",
            Specialization = "DB Magic",
            Hospital = "Container Hospital"
        };

        var response = await _client.PostAsJsonAsync("/doctors", request);

        response.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DoctorDbContext>();

        var doctor = await db.Doctors.ToListAsync();

        doctor.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ApproveDoctor_Should_Persist_In_Postgres()
    {
        var request = new CreateDoctorRequest
        {
            FullName = "Dr PG Approve",
            Specialization = "Test",
            Hospital = "DB"
        };

        await _client.PostAsJsonAsync("/doctors", request);

        var doctors = await _client.GetFromJsonAsync<List<DoctorResponse>>("/doctors");

        var id = doctors!.First().Id;

        await _client.PutAsync($"/doctors/{id}/approve", null);

        var approved = await _client.GetFromJsonAsync<List<DoctorResponse>>("/doctors/approved");

        approved.Should().Contain(d => d.Id == id && d.IsApproved);
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
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<DoctorDbContext>(options =>
                    options.UseNpgsql(_db.GetConnectionString()));
            });
        }
    }
}

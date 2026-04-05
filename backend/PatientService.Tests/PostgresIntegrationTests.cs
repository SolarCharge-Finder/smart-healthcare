using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

using PatientService.Application.DTOs;
using PatientService.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Testcontainers.PostgreSql;

namespace PatientService.Tests;

public class PatientPostgresTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _db;
    private readonly HttpClient _client;
    private readonly PostgreSqlTestingFactory _factory;

    public PatientPostgresTests()
    {
        _db = new PostgreSqlBuilder("postgres:15")
            .WithDatabase("patientdb")
            .WithUsername("test")
            .WithPassword("test")
            .Build();

        _factory = new PostgreSqlTestingFactory(_db);
        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _db.StartAsync();

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

    private async Task EnsurePatientExists()
    {
        var response = await _client.PostAsJsonAsync("/patient", new CreatePatientRequest
        {
            FullName = "Test User",
            Email = "test@test.com"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var check = await _client.GetAsync("/patient/me");
        check.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_Should_Work()
    {
        var response = await _client.PostAsJsonAsync("/patient", new CreatePatientRequest
        {
            FullName = "Create Test",
            Email = "create@test.com"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMe_Should_Return_Profile()
    {
        await EnsurePatientExists();

        var response = await _client.GetAsync("/patient/me");

        response.EnsureSuccessStatusCode();

        var patient = await response.Content.ReadFromJsonAsync<PatientResponse>();

        patient.Should().NotBeNull();
        patient!.FullName.Should().Be("Test User");
    }

    [Fact]
    public async Task Update_Should_Work()
    {
        await EnsurePatientExists();

        var update = await _client.PutAsJsonAsync("/patient", new UpdatePatientRequest
        {
            FullName = "Updated Name"
        });

        update.EnsureSuccessStatusCode();

        var patient = await _client.GetFromJsonAsync<PatientResponse>("/patient/me");

        patient!.FullName.Should().Be("Updated Name");
    }

    [Fact]
    public async Task Deactivate_Should_Work()
    {
        await EnsurePatientExists();

        var deactivate = await _client.PatchAsync("/patient/me/deactivate", null);
        deactivate.EnsureSuccessStatusCode();

        var response = await _client.GetAsync("/patient/me");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAll_Should_Work()
    {
        await EnsurePatientExists();

        var response = await _client.GetAsync("/patient");
        response.EnsureSuccessStatusCode();

        var patients = await response.Content.ReadFromJsonAsync<List<PatientResponse>>();

        patients.Should().NotBeNull();
        patients!.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Delete_Should_Work()
    {
        await EnsurePatientExists();

        var patients = await _client.GetFromJsonAsync<List<PatientResponse>>("/patient");
        var id = patients!.First().Id;

        var delete = await _client.DeleteAsync($"/patient/{id}");
        delete.EnsureSuccessStatusCode();

        var response = await _client.GetAsync($"/patient/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
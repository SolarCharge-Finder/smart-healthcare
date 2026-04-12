using System.Net;
using System.Net.Http.Json;

using Doctor.Application.DTOs;
using Doctor.Infrastructure.Data;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Testcontainers.PostgreSql;

using Xunit;

namespace DoctorService.Tests;

public class PostgresIntegrationTests : IAsyncLifetime
{
    private PostgreSqlContainer _db = null!;
    private HttpClient _client = null!;
    private PostgreSqlTestingFactory _factory = null!;

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
    public async Task CreateDoctor_Should_Persist_In_Postgres()
    {
        var request = new CreateDoctorRequest
        {
            FullName = "Dr Postgres",
            Specialization = "DB Magic",
            Hospital = "Container Hospital"
        };

        var response = await _client.PostAsJsonAsync("/doctors", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DoctorDbContext>();

        var doctors = await db.Doctors.ToListAsync();

        doctors.Should().Contain(d => d.FullName == "Dr Postgres");
    }

    [Fact]
    public async Task ApproveDoctor_Should_Update_IsApproved_In_Database()
    {
        var create = await _client.PostAsJsonAsync("/doctors", new CreateDoctorRequest
        {
            FullName = "Dr PG Approve",
            Specialization = "Test",
            Hospital = "DB"
        });

        create.EnsureSuccessStatusCode();

        var doctors = await _client.GetFromJsonAsync<List<DoctorResponse>>("/doctors");
        var doctor = doctors!.Single(d => d.FullName == "Dr PG Approve");

        var approveResponse = await _client.PutAsync($"/doctors/{doctor.Id}/approve", null);

        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DoctorDbContext>();

        var updated = await db.Doctors.FirstAsync(d => d.Id == doctor.Id);

        updated.IsApproved.Should().BeTrue();
    }

    [Fact]
    public async Task GetApproved_Should_Return_Approved_Doctors_From_Postgres()
    {
        var create = await _client.PostAsJsonAsync("/doctors", new CreateDoctorRequest
        {
            FullName = "Dr PG Approved",
            Specialization = "Cardio",
            Hospital = "DB"
        });

        create.EnsureSuccessStatusCode();

        var doctors = await _client.GetFromJsonAsync<List<DoctorResponse>>("/doctors");
        var doctor = doctors!.Single(d => d.FullName == "Dr PG Approved");

        await _client.PutAsync($"/doctors/{doctor.Id}/approve", null);

        var response = await _client.GetAsync("/doctors/approved");

        response.EnsureSuccessStatusCode();

        var approved = await response.Content.ReadFromJsonAsync<List<DoctorResponse>>();

        approved.Should().NotBeNull();
        approved!.Should().Contain(d => d.Id == doctor.Id && d.IsApproved);
    }

    [Fact]
    public async Task DeleteDoctor_Should_Remove_From_Postgres()
    {
        var create = await _client.PostAsJsonAsync("/doctors", new CreateDoctorRequest
        {
            FullName = "Dr PG Delete",
            Specialization = "Ortho",
            Hospital = "DB"
        });

        create.EnsureSuccessStatusCode();

        var doctors = await _client.GetFromJsonAsync<List<DoctorResponse>>("/doctors");
        var doctor = doctors!.Single(d => d.FullName == "Dr PG Delete");

        var delete = await _client.DeleteAsync($"/doctors/{doctor.Id}");

        delete.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DoctorDbContext>();

        var exists = await db.Doctors.AnyAsync(d => d.Id == doctor.Id);

        exists.Should().BeFalse();
    }
}
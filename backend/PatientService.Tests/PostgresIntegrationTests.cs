using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using PatientService.Application.DTOs;
using PatientService.Infrastructure.Data;

using PatientService.Tests.Fakes;

using Shared.Contracts.Enums;

using Testcontainers.PostgreSql;

using Xunit;

namespace PatientService.Tests;

public class PatientPostgresTests : IAsyncLifetime
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

    private async Task<Guid> CreatePatient(string name, string email, string? userId = null)
    {
        _client.DefaultRequestHeaders.Remove("x-user-id");

        userId ??= Guid.NewGuid().ToString();
        _client.DefaultRequestHeaders.Add("x-user-id", userId);

        var response = await _client.PostAsJsonAsync("/patient", new CreatePatientRequest
        {
            FullName = name,
            Email = email
        });

        response.EnsureSuccessStatusCode();

        var patients = await _client.GetFromJsonAsync<List<PatientResponse>>("/patient");

        var patient = patients!.First(p => p.FullName == name);
        return patient.Id;
    }

    [Fact]
    public async Task CreatePatient_Should_Persist_In_Postgres_And_AssignRole()
    {
        var id = await CreatePatient("Create Test", "create@test.com");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatientDbContext>();

        var patient = await db.Patients.FirstOrDefaultAsync(p => p.Id == id);

        patient.Should().NotBeNull();
        patient!.FullName.Should().Be("Create Test");
        patient.Email.Should().Be("create@test.com");

        // verify role assignment (fake client)
        FakeAuthServiceClient.LastAssignedRole.Should().Be(UserRole.Patient);
    }

    [Fact]
    public async Task GetMe_Should_Return_Profile_From_Database()
    {
        await CreatePatient("Getme User", "getme@test.com");

        var response = await _client.GetAsync("/patient/me");
        response.EnsureSuccessStatusCode();

        var patient = await response.Content.ReadFromJsonAsync<PatientResponse>();

        patient.Should().NotBeNull();
        patient!.FullName.Should().Be("Getme User");
    }

    [Fact]
    public async Task UpdatePatient_Should_Update_In_Postgres()
    {
        var id = await CreatePatient("Update User", "update@test.com");

        var update = await _client.PutAsJsonAsync("/patient", new UpdatePatientRequest
        {
            FullName = "Updated Name"
        });

        update.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatientDbContext>();

        var updated = await db.Patients.FirstAsync(p => p.Id == id);

        updated.FullName.Should().Be("Updated Name");
        updated.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeactivatePatient_Should_Set_IsActive_False()
    {
        var id = await CreatePatient("Deactivate User", "deactivate@test.com");

        var deactivate = await _client.PatchAsync("/patient/me/deactivate", null);
        deactivate.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatientDbContext>();

        var patient = await db.Patients.FirstAsync(p => p.Id == id);

        patient.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetAll_Should_Return_Only_Active_Patients()
    {
        await CreatePatient("Active User", "a@test.com");
        var id = await CreatePatient("Inactive User", "b@test.com");

        await _client.PatchAsync("/patient/me/deactivate", null);

        var response = await _client.GetAsync("/patient");
        response.EnsureSuccessStatusCode();

        var patients = await response.Content.ReadFromJsonAsync<List<PatientResponse>>();

        patients.Should().NotBeNull();
        patients!.Should().Contain(p => p.FullName == "Active User");
        patients.Should().NotContain(p => p.FullName == "Inactive User");
    }

    [Fact]
    public async Task DeletePatient_Should_Remove_From_Postgres()
    {
        var id = await CreatePatient("Delete User", "delete@test.com");

        var delete = await _client.DeleteAsync($"/patient/{id}");
        delete.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatientDbContext>();

        var exists = await db.Patients.AnyAsync(p => p.Id == id);

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task CreatePatient_Should_Not_Create_Duplicate_For_Same_User()
    {
        var userId = Guid.NewGuid().ToString();

        var firstId = await CreatePatient("Duplicate User", "dup@test.com", userId);
        var secondId = await CreatePatient("Duplicate User", "dup@test.com", userId);

        firstId.Should().Be(secondId);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatientDbContext>();

        var count = await db.Patients.CountAsync(p => p.Email == "dup@test.com");

        count.Should().Be(1);
    }
}

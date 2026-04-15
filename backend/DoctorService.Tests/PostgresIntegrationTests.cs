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

    // helper to set consistent user identity for DoctorOwner policy
    private void SetUser(string userId, string? role = "Admin")
    {
        _client.DefaultRequestHeaders.Remove("x-user-id");
        _client.DefaultRequestHeaders.Add("x-user-id", userId);
        _client.DefaultRequestHeaders.Remove("x-user-role");
        _client.DefaultRequestHeaders.Add("x-user-role", role);
    }

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

    // doctor search tests 
    [Fact]
    public async Task SearchDoctors_Should_Filter_By_Name_In_Postgres()
    {
        // create + approve doctor
        await _client.PostAsJsonAsync("/doctors", new CreateDoctorRequest
        {
            FullName = "Dr Search Name",
            Specialization = "General",
            Hospital = "Test"
        });

        var doctors = await _client.GetFromJsonAsync<List<DoctorResponse>>("/doctors");
        var doctor = doctors!.Single(d => d.FullName == "Dr Search Name");

        await _client.PutAsync($"/doctors/{doctor.Id}/approve", null);

        // act
        var response = await _client.GetAsync("/doctors/search?name=search");

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<List<DoctorResponse>>();

        // assert (case-insensitive via ILike)
        result.Should().Contain(d => d.Id == doctor.Id);
    }

    [Fact]
    public async Task SearchDoctors_Should_Filter_By_Specialization_In_Postgres()
    {
        await _client.PostAsJsonAsync("/doctors", new CreateDoctorRequest
        {
            FullName = "Dr Spec",
            Specialization = "Cardiology",
            Hospital = "Test"
        });

        var doctors = await _client.GetFromJsonAsync<List<DoctorResponse>>("/doctors");
        var doctor = doctors!.Single(d => d.FullName == "Dr Spec");

        await _client.PutAsync($"/doctors/{doctor.Id}/approve", null);

        var response = await _client.GetAsync("/doctors/search?specialization=cardio");

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<List<DoctorResponse>>();

        result.Should().Contain(d => d.Id == doctor.Id);
    }

    [Fact]
    public async Task SearchDoctors_Should_Filter_By_Hospital_And_Date_In_Postgres()
    {
        // use consistent user (required for DoctorOwner policy)
        var userId = Guid.NewGuid().ToString();
        SetUser(userId, "Doctor");

        // create doctor
        await _client.PostAsJsonAsync("/doctors", new CreateDoctorRequest
        {
            FullName = "Dr Availability PG",
            Specialization = "General",
            Hospital = "Main Hospital"
        });

        // admin approves doctor
        SetUser(Guid.NewGuid().ToString(), "Admin"); // switch to admin user for approval

        var doctors = await _client.GetFromJsonAsync<List<DoctorResponse>>("/doctors");
        var doctor = doctors!.Single(d => d.FullName == "Dr Availability PG");

        var approveResponse = await _client.PutAsync($"/doctors/{doctor.Id}/approve", null);
        approveResponse.EnsureSuccessStatusCode();

        // create availability (must be same user)
        SetUser(userId, "Doctor"); // switch back to doctor user

        var targetDate = DateTime.UtcNow.Date.AddDays(1);

        var start = DateTime.SpecifyKind(targetDate.AddHours(9), DateTimeKind.Utc);
        var end = DateTime.SpecifyKind(targetDate.AddHours(12), DateTimeKind.Utc);

        var availabilityResponse = await _client.PostAsJsonAsync(
            $"/doctors/{doctor.Id}/availability",
            new CreateAvailability
            {
                Hospital = "Main Hospital",
                StartTime = start,
                EndTime = end,
                IsRecurring = false
            });

        // ensure availability was actually created (important for debugging)
        availabilityResponse.EnsureSuccessStatusCode();

        // act
        var query = $"hospital=Main&date={targetDate:yyyy-MM-dd}";
        var response = await _client.GetAsync($"/doctors/search?{query}");

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<List<DoctorResponse>>();

        // assert doctor is returned
        result.Should().Contain(d => d.Id == doctor.Id);
    }

    [Fact]
    public async Task SearchDoctors_Should_Exclude_Doctors_Without_Availability_On_Date_In_Postgres()
    {
        await _client.PostAsJsonAsync("/doctors", new CreateDoctorRequest
        {
            FullName = "Dr No Availability PG",
            Specialization = "General",
            Hospital = "Test"
        });

        var doctors = await _client.GetFromJsonAsync<List<DoctorResponse>>("/doctors");
        var doctor = doctors!.Single(d => d.FullName == "Dr No Availability PG");

        await _client.PutAsync($"/doctors/{doctor.Id}/approve", null);

        var targetDate = DateTime.UtcNow.Date.AddDays(2);

        var response = await _client.GetAsync($"/doctors/search?date={targetDate:yyyy-MM-dd}");

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<List<DoctorResponse>>();

        result.Should().NotContain(d => d.Id == doctor.Id);
    }

    [Fact]
    public async Task SearchDoctors_Should_Return_Only_Approved_Doctors_In_Postgres()
    {
        await _client.PostAsJsonAsync("/doctors", new CreateDoctorRequest
        {
            FullName = "Dr Not Approved PG",
            Specialization = "General",
            Hospital = "Test"
        });

        var response = await _client.GetAsync("/doctors/search");

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<List<DoctorResponse>>();

        result.Should().NotContain(d => d.FullName == "Dr Not Approved PG");
    }

    [Fact]
    public async Task SearchDoctors_Should_Handle_Combined_Filters_In_Postgres()
    {
        // use consistent user (required for DoctorOwner policy)
        var userId = Guid.NewGuid().ToString();
        SetUser(userId, "Undefined");

        // create doctor
        await _client.PostAsJsonAsync("/doctors", new CreateDoctorRequest
        {
            FullName = "Dr Combined PG",
            Specialization = "Dermatology",
            Hospital = "Central Hospital"
        });

        // approve doctor
        SetUser(Guid.NewGuid().ToString(), "Admin"); // switch to admin user for approval

        var doctors = await _client.GetFromJsonAsync<List<DoctorResponse>>("/doctors");
        var doctor = doctors!.Single(d => d.FullName == "Dr Combined PG");

        var approveResponse = await _client.PutAsync($"/doctors/{doctor.Id}/approve", null);
        approveResponse.EnsureSuccessStatusCode();

        // create availability (same user required)
        SetUser(userId, "Doctor"); // switch back to doctor user

        var targetDate = DateTime.UtcNow.Date.AddDays(1);

        var start = DateTime.SpecifyKind(targetDate.AddHours(10), DateTimeKind.Utc);
        var end = DateTime.SpecifyKind(targetDate.AddHours(12), DateTimeKind.Utc);

        var availabilityResponse = await _client.PostAsJsonAsync(
            $"/doctors/{doctor.Id}/availability",
            new CreateAvailability
            {
                Hospital = "Central Hospital",
                StartTime = start,
                EndTime = end,
                IsRecurring = false
            });

        availabilityResponse.EnsureSuccessStatusCode();

        // act: combined filters (real-world scenario)
        var query = $"name=combined&specialization=derma&hospital=central&date={targetDate:yyyy-MM-dd}";
        var response = await _client.GetAsync($"/doctors/search?{query}");

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<List<DoctorResponse>>();

        // assert doctor is returned only when ALL filters match
        result.Should().Contain(d => d.Id == doctor.Id);
    }

    // filter option test
    [Fact]
    public async Task GetFilterOptions_Should_Work_In_Postgres()
    {
        // create doctors
        SetUser(Guid.NewGuid().ToString(), "Doctor"); 
        await _client.PostAsJsonAsync("/doctors", new CreateDoctorRequest
        {
            FullName = "Dr PG A",
            Specialization = "Cardiology",
            Hospital = "Hospital PG"
        });

        SetUser(Guid.NewGuid().ToString(), "Doctor");
        await _client.PostAsJsonAsync("/doctors", new CreateDoctorRequest
        {
            FullName = "Dr PG B",
            Specialization = "Dermatology",
            Hospital = "Hospital PG"
        });

        SetUser(Guid.NewGuid().ToString(), "Admin");

        // approve doctors
        var doctors = await _client.GetFromJsonAsync<List<DoctorResponse>>("/doctors");

        foreach (var doctor in doctors!)
        {
            await _client.PutAsync($"/doctors/{doctor.Id}/approve", null);
        }

        // act
        var response = await _client.GetAsync("/doctors/filter-options");

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<FilterOptionsResponse>();

        // assert
        result.Should().NotBeNull();

        result!.DoctorNames.Should().Contain(new[] { "Dr PG A", "Dr PG B" });

        result.Specializations.Should().Contain("Cardiology");
        result.Specializations.Should().Contain("Dermatology");

        // hospital should appear only once
        result.Hospitals.Should().Contain("Hospital PG");
        result.Hospitals.Count.Should().Be(1);
    }
}

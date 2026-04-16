using System.Net;
using System.Net.Http.Json;

using AdminService.Application.DTOs;
using AdminService.Infrastructure.Data;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Testcontainers.PostgreSql;

using Xunit;

namespace AdminService.Tests;

public class PostgresIntegrationTests : IAsyncLifetime
{
    private PostgreSqlContainer _db = null!;
    private HttpClient _client = null!;
    private PostgreSqlTestingFactory _factory = null!;

    // helper to simulate user identity
    private void SetUser(string userId, string role = "Admin")
    {
        _client.DefaultRequestHeaders.Remove("x-user-id");
        _client.DefaultRequestHeaders.Add("x-user-id", userId);

        _client.DefaultRequestHeaders.Remove("x-user-role");
        _client.DefaultRequestHeaders.Add("x-user-role", role);
    }

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
    public async Task CreateAdmin_Should_Persist_In_Postgres()
    {
        SetUser(Guid.NewGuid().ToString(), "Undefined");

        await _client.PostAsJsonAsync("/admin", new CreateAdminRequest
        {
            FullName = "PG Admin"
        });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();

        var admins = await db.Admins.ToListAsync();

        admins.Should().Contain(a => a.FullName == "PG Admin");
    }

    [Fact]
    public async Task ApproveAdmin_Should_Update_IsApproved_In_Database()
    {
        SetUser(Guid.NewGuid().ToString(), "Undefined");

        await _client.PostAsJsonAsync("/admin", new CreateAdminRequest
        {
            FullName = "PG Approve Admin"
        });

        SetUser(Guid.NewGuid().ToString(), "Admin");

        var admins = await _client.GetFromJsonAsync<List<AdminResponse>>("/admin");
        var admin = admins!.Single(a => a.FullName == "PG Approve Admin");

        var response = await _client.PutAsync($"/admin/{admin.Id}/approve", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();

        var updated = await db.Admins.FirstAsync(a => a.Id == admin.Id);

        updated.IsApproved.Should().BeTrue();
    }

    [Fact]
    public async Task RejectAdmin_Should_Remove_From_Postgres()
    {
        SetUser(Guid.NewGuid().ToString(), "Undefined");

        await _client.PostAsJsonAsync("/admin", new CreateAdminRequest
        {
            FullName = "PG Reject Admin"
        });

        SetUser(Guid.NewGuid().ToString(), "Admin");

        var admins = await _client.GetFromJsonAsync<List<AdminResponse>>("/admin");
        var admin = admins!.Single(a => a.FullName == "PG Reject Admin");

        var response = await _client.DeleteAsync($"/admin/{admin.Id}/reject");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();

        var exists = await db.Admins.AnyAsync(a => a.Id == admin.Id);

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task GetPending_Should_Return_Only_Unapproved_Admins_From_Postgres()
    {
        SetUser(Guid.NewGuid().ToString(), "Undefined");

        await _client.PostAsJsonAsync("/admin", new CreateAdminRequest
        {
            FullName = "PG Pending Admin"
        });

        SetUser(Guid.NewGuid().ToString(), "Admin");

        var response = await _client.GetAsync("/admin/pending");

        response.EnsureSuccessStatusCode();

        var admins = await response.Content.ReadFromJsonAsync<List<AdminResponse>>();

        admins.Should().NotBeNull();
        admins!.All(a => a.IsApproved == false).Should().BeTrue();
    }

    [Fact]
    public async Task GetAll_Should_Return_Data_From_Postgres()
    {
        SetUser(Guid.NewGuid().ToString(), "Undefined");

        await _client.PostAsJsonAsync("/admin", new CreateAdminRequest
        {
            FullName = "PG Admin List"
        });

        SetUser(Guid.NewGuid().ToString(), "Admin");

        var response = await _client.GetAsync("/admin");

        response.EnsureSuccessStatusCode();

        var admins = await response.Content.ReadFromJsonAsync<List<AdminResponse>>();

        admins.Should().NotBeNull();
        admins.Should().Contain(a => a.FullName == "PG Admin List");
    }

    [Fact]
    public async Task ApproveDoctor_Should_Update_Doctor_Status_Via_Admin()
    {
        using var scope = _factory.Services.CreateScope();

        var fake = scope.ServiceProvider
            .GetRequiredService<FakeDoctorServiceClient>();

        var doctor = new DoctorResponse
        {
            Id = Guid.NewGuid(),
            FullName = "PG Doctor",
            IsApproved = false
        };

        fake.Seed(doctor);

        var response = await _client.PutAsync($"/admin/doctors/{doctor.Id}/approve", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var pending = await _client.GetFromJsonAsync<List<DoctorResponse>>("/admin/doctors/pending");

        pending!.Should().NotContain(d => d.Id == doctor.Id);
    }

    [Fact]
    public async Task CreateAdmin_Should_Fail_For_Duplicate_User_In_Postgres()
    {
        var userId = Guid.NewGuid().ToString();
        SetUser(userId, "Undefined");

        var request = new CreateAdminRequest
        {
            FullName = "Duplicate PG Admin"
        };

        await _client.PostAsJsonAsync("/admin", request);

        var response = await _client.PostAsJsonAsync("/admin", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

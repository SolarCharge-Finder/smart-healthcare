using System.Net;
using System.Net.Http.Json;

using AdminService.Application.DTOs;
using AdminService.Application.Interfaces;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace AdminService.Tests;

public class AdminTests : IClassFixture<TestingFactory>
{
    private readonly HttpClient _client;
    private readonly TestingFactory _factory;

    public AdminTests(TestingFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        factory.ResetDatabaseAsync().GetAwaiter().GetResult();
    }

    // helper to simulate user identity
    private void SetUser(string userId, string role = "Admin")
    {
        _client.DefaultRequestHeaders.Remove("x-user-id");
        _client.DefaultRequestHeaders.Add("x-user-id", userId);

        _client.DefaultRequestHeaders.Remove("x-user-role");
        _client.DefaultRequestHeaders.Add("x-user-role", role);
    }

    [Fact]
    public async Task CreateAdmin_Should_Create_Admin()
    {
        SetUser(Guid.NewGuid().ToString(), "Undefined");

        var request = new CreateAdminRequest
        {
            FullName = "Test Admin"
        };

        var response = await _client.PostAsJsonAsync("/admin", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        SetUser(Guid.NewGuid().ToString(), "Admin");

        var admins = await _client.GetFromJsonAsync<List<AdminResponse>>("/admin");

        admins.Should().Contain(a => a.FullName == "Test Admin");
    }

    [Fact]
    public async Task CreateAdmin_Should_Fail_If_Already_Exists()
    {
        SetUser("same-user", "Undefined");

        var request = new CreateAdminRequest
        {
            FullName = "Duplicate Admin"
        };

        await _client.PostAsJsonAsync("/admin", request);

        var response = await _client.PostAsJsonAsync("/admin", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAll_Should_Return_Admin_List()
    {
        SetUser(Guid.NewGuid().ToString(), "Undefined");

        await _client.PostAsJsonAsync("/admin", new CreateAdminRequest
        {
            FullName = "Admin One"
        });

        SetUser(Guid.NewGuid().ToString(), "Admin");

        var response = await _client.GetAsync("/admin");

        response.EnsureSuccessStatusCode();

        var admins = await response.Content.ReadFromJsonAsync<List<AdminResponse>>();

        admins.Should().NotBeNull();
        admins.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetPending_Should_Return_Only_Unapproved_Admins()
    {
        SetUser(Guid.NewGuid().ToString(), "Undefined");

        await _client.PostAsJsonAsync("/admin", new CreateAdminRequest
        {
            FullName = "Pending Admin"
        });

        SetUser(Guid.NewGuid().ToString(), "Admin");

        var response = await _client.GetAsync("/admin/pending");

        response.EnsureSuccessStatusCode();

        var admins = await response.Content.ReadFromJsonAsync<List<AdminResponse>>();

        admins.Should().NotBeNull();
        admins!.All(a => a.IsApproved == false).Should().BeTrue();
    }

    [Fact]
    public async Task ApproveAdmin_Should_Set_IsApproved_True()
    {
        SetUser(Guid.NewGuid().ToString(), "Undefined");

        await _client.PostAsJsonAsync("/admin", new CreateAdminRequest
        {
            FullName = "Approve Me"
        });

        SetUser(Guid.NewGuid().ToString(), "Admin");

        var admins = await _client.GetFromJsonAsync<List<AdminResponse>>("/admin");
        var admin = admins!.Single(a => a.FullName == "Approve Me");

        var response = await _client.PutAsync($"/admin/{admin.Id}/approve", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await _client.GetFromJsonAsync<AdminResponse>($"/admin/{admin.Id}");

        updated!.IsApproved.Should().BeTrue();
    }

    [Fact]
    public async Task RejectAdmin_Should_Remove_Admin()
    {
        SetUser(Guid.NewGuid().ToString(), "Undefined");

        await _client.PostAsJsonAsync("/admin", new CreateAdminRequest
        {
            FullName = "Reject Me"
        });

        SetUser(Guid.NewGuid().ToString(), "Admin");

        var admins = await _client.GetFromJsonAsync<List<AdminResponse>>("/admin");
        var admin = admins!.Single(a => a.FullName == "Reject Me");

        var response = await _client.DeleteAsync($"/admin/{admin.Id}/reject");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await _client.GetAsync($"/admin/{admin.Id}");

        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPendingDoctors_Should_Return_Data()
    {
        var response = await _client.GetAsync("/admin/doctors/pending");

        response.EnsureSuccessStatusCode();

        var doctors = await response.Content.ReadFromJsonAsync<List<DoctorResponse>>();

        doctors.Should().NotBeNull();
    }

    [Fact]
    public async Task ApproveDoctor_Should_Work_And_Set_Approved()
    {
        SetUser(Guid.NewGuid().ToString(), "Undefined");

        // get fake service from DI
        using var scope = _factory.Services.CreateScope();

        var fake = (FakeDoctorServiceClient)scope.ServiceProvider
            .GetRequiredService<IDoctorServiceClient>();

        var doctor = new DoctorResponse
        {
            Id = Guid.NewGuid(),
            FullName = "Dr Pending",
            IsApproved = false
        };

        fake.Seed(doctor);

        SetUser(Guid.NewGuid().ToString(), "Admin");

        // verify doctor appears in pending
        var doctors = await _client.GetFromJsonAsync<List<DoctorResponse>>("/admin/doctors/pending");
        doctors!.Should().Contain(d => d.FullName == "Dr Pending");

        // approve doctor
        var response = await _client.PutAsync($"/admin/doctors/{doctor.Id}/approve", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // verify doctor is no longer pending
        var updated = await _client.GetFromJsonAsync<List<DoctorResponse>>("/admin/doctors/pending");
        updated!.Should().NotContain(d => d.Id == doctor.Id);
    }
}

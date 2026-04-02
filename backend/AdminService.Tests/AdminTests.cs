using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;
using AdminService.Application.DTOs;

namespace AdminService.Tests;

public class AdminTests
{
    private readonly HttpClient _client;

    public AdminTests()
    {
        var factory = new TestingFactory();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateAdmin_Should_Return_Success()
    {
        var request = new CreateAdminRequest
        {
            FullName = "Test Admin"
        };

        var response = await _client.PostAsJsonAsync("/admin", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
    [Fact]
    public async Task GetAll_Should_Return_Admin_List()
    {
        // Arrange
        var request = new CreateAdminRequest
        {
            FullName = "Admin One"
        };

        await _client.PostAsJsonAsync("/admin", request);

        // Act
        var response = await _client.GetAsync("/admin");

        // Assert
        response.EnsureSuccessStatusCode();

        var admins = await response.Content.ReadFromJsonAsync<List<AdminResponse>>();

        Assert.NotNull(admins);
        Assert.NotEmpty(admins);
    }
    [Fact]
    public async Task CreateAdmin_Should_Fail_If_Already_Exists()
    {
        var request = new CreateAdminRequest
        {
            FullName = "Duplicate Admin"
        };

        // First create
        await _client.PostAsJsonAsync("/admin", request);

        // Second create (same user)
        var response = await _client.PostAsJsonAsync("/admin", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    [Fact]
    public async Task GetPending_Should_Return_Unapproved_Admins()
    {
        var request = new CreateAdminRequest
        {
            FullName = "Pending Admin"
        };

        await _client.PostAsJsonAsync("/admin", request);

        var response = await _client.GetAsync("/admin/pending");

        response.EnsureSuccessStatusCode();

        var admins = await response.Content.ReadFromJsonAsync<List<AdminResponse>>();

        Assert.NotNull(admins);
        Assert.All(admins!, a => Assert.False(a.IsApproved));
    }
    [Fact]
    public async Task ApproveAdmin_Should_Work()
    {
        var create = await _client.PostAsJsonAsync("/admin", new CreateAdminRequest
        {
            FullName = "Approve Me"
        });

        var admins = await _client.GetFromJsonAsync<List<AdminResponse>>("/admin");

        var id = admins!.Single(a => a.FullName == "Approve Me").Id;

        var response = await _client.PutAsync($"/admin/{id}/approve", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
    [Fact]
    public async Task GetPendingDoctors_Should_Return_Data()
    {
        var response = await _client.GetAsync("/admin/doctors/pending");

        response.EnsureSuccessStatusCode();

        var doctors = await response.Content.ReadFromJsonAsync<List<DoctorResponse>>();

        doctors.Should().NotBeNull();
        doctors.Should().NotBeEmpty();
    }
    [Fact]
    public async Task ApproveDoctor_Should_Return_OK()
    {
        var id = Guid.NewGuid();

        var response = await _client.PutAsync($"/admin/doctors/{id}/approve", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}


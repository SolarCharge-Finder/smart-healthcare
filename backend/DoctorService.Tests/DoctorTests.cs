using System.Net;
using System.Net.Http.Json;

using Doctor.Application.DTOs;

using FluentAssertions;

using Xunit;

namespace DoctorService.Tests;

public class DoctorTests : IClassFixture<TestingFactory>
{
    private readonly HttpClient _client;

    public DoctorTests(TestingFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateDoctor_Should_Return_OK_And_Create_Doctor()
    {
        var request = new CreateDoctorRequest
        {
            FullName = "Dr Strange",
            Specialization = "Magic",
            Hospital = "Kamar Taj"
        };

        var response = await _client.PostAsJsonAsync("/doctors", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var doctors = await _client.GetFromJsonAsync<List<DoctorResponse>>("/doctors");

        doctors.Should().Contain(d => d.FullName == "Dr Strange");
    }

    [Fact]
    public async Task ApproveDoctor_Should_Set_IsApproved_True()
    {
        var create = await _client.PostAsJsonAsync("/doctors", new CreateDoctorRequest
        {
            FullName = "Dr Who",
            Specialization = "Time Travel",
            Hospital = "TARDIS"
        });

        create.EnsureSuccessStatusCode();

        var doctors = await _client.GetFromJsonAsync<List<DoctorResponse>>("/doctors");
        var doctor = doctors!.Single(d => d.FullName == "Dr Who");

        var response = await _client.PutAsync($"/doctors/{doctor.Id}/approve", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await _client.GetFromJsonAsync<DoctorResponse>($"/doctors/{doctor.Id}");

        updated!.IsApproved.Should().BeTrue();
    }

    [Fact]
    public async Task GetById_Should_Return_Correct_Doctor()
    {
        var create = await _client.PostAsJsonAsync("/doctors", new CreateDoctorRequest
        {
            FullName = "Dr Lookup",
            Specialization = "ENT",
            Hospital = "Test"
        });

        create.EnsureSuccessStatusCode();

        var doctors = await _client.GetFromJsonAsync<List<DoctorResponse>>("/doctors");
        var doctor = doctors!.Single(d => d.FullName == "Dr Lookup");

        var response = await _client.GetAsync($"/doctors/{doctor.Id}");

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<DoctorResponse>();

        result.Should().NotBeNull();
        result!.Id.Should().Be(doctor.Id);
    }

    [Fact]
    public async Task GetApproved_Should_Return_Only_Approved_Doctors()
    {
        var create = await _client.PostAsJsonAsync("/doctors", new CreateDoctorRequest
        {
            FullName = "Dr Approved",
            Specialization = "Neuro",
            Hospital = "Test"
        });

        create.EnsureSuccessStatusCode();

        var doctors = await _client.GetFromJsonAsync<List<DoctorResponse>>("/doctors");
        var doctor = doctors!.Single(d => d.FullName == "Dr Approved");

        await _client.PutAsync($"/doctors/{doctor.Id}/approve", null);

        var response = await _client.GetAsync("/doctors/approved");

        response.EnsureSuccessStatusCode();

        var approved = await response.Content.ReadFromJsonAsync<List<DoctorResponse>>();

        approved.Should().NotBeNull();
        approved!.Should().Contain(d => d.Id == doctor.Id && d.IsApproved);
        approved.All(d => d.IsApproved).Should().BeTrue();
    }

    [Fact]
    public async Task GetPending_Should_Return_Only_Unapproved_Doctors()
    {
        var request = new CreateDoctorRequest
        {
            FullName = "Dr House",
            Specialization = "Diagnostics",
            Hospital = "Princeton"
        };

        await _client.PostAsJsonAsync("/doctors", request);

        var response = await _client.GetAsync("/doctors/pending");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var doctors = await response.Content.ReadFromJsonAsync<List<DoctorResponse>>();

        doctors.Should().NotBeNull();
        doctors.Should().NotBeEmpty();
        doctors!.All(d => d.IsApproved == false).Should().BeTrue();
    }

    [Fact]
    public async Task DeleteDoctor_Should_Remove_Doctor_Completely()
    {
        var create = await _client.PostAsJsonAsync("/doctors", new CreateDoctorRequest
        {
            FullName = "Dr Delete",
            Specialization = "Ortho",
            Hospital = "Test"
        });

        create.EnsureSuccessStatusCode();

        var doctors = await _client.GetFromJsonAsync<List<DoctorResponse>>("/doctors");
        var doctor = doctors!.Single(d => d.FullName == "Dr Delete");

        var delete = await _client.DeleteAsync($"/doctors/{doctor.Id}");

        delete.EnsureSuccessStatusCode();

        var response = await _client.GetAsync($"/doctors/{doctor.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

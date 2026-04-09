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
    public async Task CreateDoctor_Should_Return_OK()
    {
        var request = new CreateDoctorRequest
        {
            FullName = "Dr Strange",
            Specialization = "Magic",
            Hospital = "Kamar Taj"
        };

        var response = await _client.PostAsJsonAsync("/doctors", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ApproveDoctor_Should_Work()
    {
        var request = new CreateDoctorRequest
        {
            FullName = "Dr Who",
            Specialization = "Time Travel",
            Hospital = "TARDIS"
        };

        await _client.PostAsJsonAsync("/doctors", request);

        var doctors = await _client.GetFromJsonAsync<List<DoctorResponse>>("/doctors");

        var id = doctors!.First().Id;

        var response = await _client.PutAsync($"/doctors/{id}/approve", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_Should_Return_Doctor()
    {
        var create = await _client.PostAsJsonAsync("/doctors", new CreateDoctorRequest
        {
            FullName = "Dr Lookup",
            Specialization = "ENT",
            Hospital = "Test"
        });

        create.EnsureSuccessStatusCode();

        var all = await _client.GetFromJsonAsync<List<DoctorResponse>>("/doctors");

        var id = all!.First().Id;

        var response = await _client.GetAsync($"/doctors/{id}");

        response.EnsureSuccessStatusCode();

        var doctor = await response.Content.ReadFromJsonAsync<DoctorResponse>();

        doctor.Should().NotBeNull();
        doctor!.Id.Should().Be(id);
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

        var all = await _client.GetFromJsonAsync<List<DoctorResponse>>("/doctors");
        var id = all!.First().Id;

        await _client.PutAsync($"/doctors/{id}/approve", null);

        var response = await _client.GetAsync("/doctors/approved");

        response.EnsureSuccessStatusCode();

        var doctors = await response.Content.ReadFromJsonAsync<List<DoctorResponse>>();

        doctors.Should().NotBeNull();
        doctors.Should().NotBeEmpty();
        doctors!.All(d => d.IsApproved).Should().BeTrue();
    }

    [Fact]
    public async Task GetPending_Should_Return_Unapproved_Doctors()
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
    public async Task DeleteDoctor_Should_Remove_Doctor()
    {
        var create = await _client.PostAsJsonAsync("/doctors", new CreateDoctorRequest
        {
            FullName = "Dr Delete",
            Specialization = "Ortho",
            Hospital = "Test"
        });

        create.EnsureSuccessStatusCode();

        var all = await _client.GetFromJsonAsync<List<DoctorResponse>>("/doctors");

        var id = all!.First().Id;

        var delete = await _client.DeleteAsync($"/doctors/{id}");

        delete.EnsureSuccessStatusCode();

        var response = await _client.GetAsync($"/doctors/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

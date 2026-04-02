using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;
using PatientService.Application.DTOs;

namespace PatientService.Tests;

public class PatientTests : IClassFixture<TestingFactory>
{
    private readonly HttpClient _client;

    public PatientTests(TestingFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreatePatient_Should_Work()
    {
        var request = new CreatePatientRequest
        {
            FullName = "Test Patient",
            Email = "test@test.com"
        };

        var response = await _client.PostAsJsonAsync("/patients", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMe_Should_Return_Patient()
    {
        var create = await _client.PostAsJsonAsync("/patients", new CreatePatientRequest
        {
            FullName = "Me",
            Email = "me@test.com"
        });

        create.EnsureSuccessStatusCode();

        var response = await _client.GetAsync("/patients/me");

        response.EnsureSuccessStatusCode();

        var patient = await response.Content.ReadFromJsonAsync<PatientResponse>();

        patient.Should().NotBeNull();
        patient!.FullName.Should().Be("Me");
    }

    [Fact]
    public async Task UpdatePatient_Should_Work()
    {
        var create = await _client.PostAsJsonAsync("/patients", new CreatePatientRequest
        {
            FullName = "Old",
            Email = "old@test.com"
        });

        create.EnsureSuccessStatusCode();

        var response = await _client.PutAsJsonAsync("/patients/me", new UpdatePatientRequest
        {
            FullName = "Updated"
        });

        response.EnsureSuccessStatusCode();

        var updated = await _client.GetFromJsonAsync<PatientResponse>("/patients/me");

        updated!.FullName.Should().Be("Updated");
    }

    [Fact]
    public async Task DeactivatePatient_Should_Work()
    {
        var create = await _client.PostAsJsonAsync("/patients", new CreatePatientRequest
        {
            FullName = "Deactivate",
            Email = "d@test.com"
        });

        create.EnsureSuccessStatusCode();

        var response = await _client.PatchAsync("/patients/me/deactivate", null);

        response.EnsureSuccessStatusCode();

        var patient = await _client.GetAsync("/patients/me");

        patient.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAll_Should_Return_Patients()
    {
        var create = await _client.PostAsJsonAsync("/patients", new CreatePatientRequest
        {
            FullName = "Admin Patient",
            Email = "admin@test.com"
        });

        create.EnsureSuccessStatusCode();

        var response = await _client.GetAsync("/patients");

        response.EnsureSuccessStatusCode();

        var patients = await response.Content.ReadFromJsonAsync<List<PatientResponse>>();

        patients.Should().NotBeNull();
        patients!.Count.Should().BeGreaterThan(0);
    }

}

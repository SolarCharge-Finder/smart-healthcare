using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using PatientService.Application.DTOs;

using Xunit;

namespace PatientService.Tests;

public class PatientTests : IClassFixture<TestingFactory>
{
    private readonly HttpClient _client;

    public PatientTests(TestingFactory factory)
    {
        _client = factory.CreateClient();
    }

    private void SetUser(string userId)
    {
        _client.DefaultRequestHeaders.Remove("x-user-id");
        _client.DefaultRequestHeaders.Add("x-user-id", userId);
    }

    [Fact]
    public async Task CreatePatient_Should_Return_OK_And_Create_Patient()
    {
        SetUser(Guid.NewGuid().ToString());

        var response = await _client.PostAsJsonAsync("/patient", new CreatePatientRequest
        {
            FullName = "Test Patient",
            Email = "test@test.com"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var patients = await _client.GetFromJsonAsync<List<PatientResponse>>("/patient");

        patients.Should().Contain(p => p.FullName == "Test Patient");
    }

    [Fact]
    public async Task GetMe_Should_Return_Correct_Patient()
    {
        var userId = Guid.NewGuid().ToString();
        SetUser(userId);

        await _client.PostAsJsonAsync("/patient", new CreatePatientRequest
        {
            FullName = "Me Patient",
            Email = "me@test.com"
        });

        // same user
        SetUser(userId);

        var response = await _client.GetAsync("/patient/me");

        response.EnsureSuccessStatusCode();

        var patient = await response.Content.ReadFromJsonAsync<PatientResponse>();

        patient.Should().NotBeNull();
        patient!.FullName.Should().Be("Me Patient");
    }

    [Fact]
    public async Task UpdatePatient_Should_Update_FullName()
    {
        var userId = Guid.NewGuid().ToString();
        SetUser(userId);

        await _client.PostAsJsonAsync("/patient", new CreatePatientRequest
        {
            FullName = "Before Update",
            Email = "update@test.com"
        });

        // same user
        SetUser(userId);

        var update = await _client.PutAsJsonAsync("/patient", new UpdatePatientRequest
        {
            FullName = "After Update"
        });

        update.EnsureSuccessStatusCode();

        var patient = await _client.GetFromJsonAsync<PatientResponse>("/patient/me");

        patient!.FullName.Should().Be("After Update");
    }

    [Fact]
    public async Task DeactivatePatient_Should_Return_NotFound_On_GetMe()
    {
        var userId = Guid.NewGuid().ToString();
        SetUser(userId);

        await _client.PostAsJsonAsync("/patient", new CreatePatientRequest
        {
            FullName = "Deactivate Me",
            Email = "deactivate@test.com"
        });

        SetUser(userId);

        await _client.PatchAsync("/patient/me/deactivate", null);

        var response = await _client.GetAsync("/patient/me");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletePatient_Should_Remove_Patient()
    {
        var userId = Guid.NewGuid().ToString();
        SetUser(userId);

        await _client.PostAsJsonAsync("/patient", new CreatePatientRequest
        {
            FullName = "Delete Me",
            Email = "delete@test.com"
        });

        var patients = await _client.GetFromJsonAsync<List<PatientResponse>>("/patient");
        var id = patients!.Single(p => p.FullName == "Delete Me").Id;

        var delete = await _client.DeleteAsync($"/patient/{id}");

        delete.EnsureSuccessStatusCode();

        var response = await _client.GetAsync($"/patient/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

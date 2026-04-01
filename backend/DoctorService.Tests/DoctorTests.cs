using System.Net;
using System.Net.Http.Json;
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
        var request = new
        {
            userId = Guid.NewGuid(),
            fullName = "Dr Strange",
            specialization = "Magic",
            hospital = "Kamar Taj"
        };

        var response = await _client.PostAsJsonAsync("/doctors", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ApproveDoctor_Should_Work()
    {
        var request = new
        {
            userId = Guid.NewGuid(),
            fullName = "Dr Who",
            specialization = "Time Travel",
            hospital = "TARDIS"
        };

        await _client.PostAsJsonAsync("/doctors", request);

        var doctors = await _client.GetFromJsonAsync<List<dynamic>>("/doctors");

        var id = doctors![0].GetProperty("id").GetGuid();

        var response = await _client.PutAsync($"/doctors/{id}/approve", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPending_Should_Return_Doctors()
    {
        var request = new
        {
            userId = Guid.NewGuid(),
            fullName = "Dr House",
            specialization = "Diagnostics",
            hospital = "Princeton"
        };

        await _client.PostAsJsonAsync("/doctors", request);

        var response = await _client.GetAsync("/doctors/pending");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var doctors = await response.Content.ReadFromJsonAsync<List<object>>();

        doctors.Should().NotBeNull();
        doctors!.Count.Should().BeGreaterThan(0);
    }
}
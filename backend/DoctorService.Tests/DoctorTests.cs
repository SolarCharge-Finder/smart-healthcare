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

    // helper to set consistent user identity for DoctorOwner policy
    private void SetUser(string userId, string? role = "Admin")
    {
        _client.DefaultRequestHeaders.Remove("x-user-id");
        _client.DefaultRequestHeaders.Add("x-user-id", userId);
        _client.DefaultRequestHeaders.Remove("x-user-role");
        _client.DefaultRequestHeaders.Add("x-user-role", role);
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

    //doctor search tests
    [Fact]
    public async Task SearchDoctors_Should_Filter_By_Name()
    {
        // create and approve doctor
        await _client.PostAsJsonAsync("/doctors", new CreateDoctorRequest
        {
            FullName = "Dr John Smith",
            Specialization = "Cardiology",
            Hospital = "City Hospital"
        });

        var doctors = await _client.GetFromJsonAsync<List<DoctorResponse>>("/doctors");
        var doctor = doctors!.Single(d => d.FullName == "Dr John Smith");

        await _client.PutAsync($"/doctors/{doctor.Id}/approve", null);

        // act
        var response = await _client.GetAsync("/doctors/search?name=John");

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<List<DoctorResponse>>();

        // assert
        result.Should().NotBeNull();
        result!.Should().Contain(d => d.FullName.Contains("John"));
    }

    [Fact]
    public async Task SearchDoctors_Should_Filter_By_Specialization()
    {
        await _client.PostAsJsonAsync("/doctors", new CreateDoctorRequest
        {
            FullName = "Dr Heart",
            Specialization = "Cardiology",
            Hospital = "City Hospital"
        });

        var doctors = await _client.GetFromJsonAsync<List<DoctorResponse>>("/doctors");
        var doctor = doctors!.Single(d => d.FullName == "Dr Heart");

        await _client.PutAsync($"/doctors/{doctor.Id}/approve", null);

        var response = await _client.GetAsync("/doctors/search?specialization=Cardio");

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<List<DoctorResponse>>();

        result.Should().NotBeNull();
        result!.Should().Contain(d => d.Specialization.Contains("Cardio"));
    }

    [Fact]
    public async Task SearchDoctors_Should_Filter_By_Hospital_And_Date()
    {
        var userId = Guid.NewGuid().ToString();
        SetUser(userId, "Doctor");

        // create doctor
        await _client.PostAsJsonAsync("/doctors", new CreateDoctorRequest
        {
            FullName = "Dr Available",
            Specialization = "General",
            Hospital = "Main Hospital"
        });

        SetUser(Guid.NewGuid().ToString(), "Admin"); // switch to admin user for approval

        var doctors = await _client.GetFromJsonAsync<List<DoctorResponse>>("/doctors");
        var doctor = doctors!.Single(d => d.FullName == "Dr Available");

        await _client.PutAsync($"/doctors/{doctor.Id}/approve", null);

        SetUser(userId, "Doctor"); // switch back to doctor user to set availability

        // add availability for specific date
        var targetDate = DateTime.UtcNow.Date.AddDays(1);
        var startTime = DateTime.SpecifyKind(targetDate.AddHours(9), DateTimeKind.Utc);
        var endTime = DateTime.SpecifyKind(targetDate.AddHours(12), DateTimeKind.Utc);

        await _client.PostAsJsonAsync($"/doctors/{doctor.Id}/availability", new
        {
            hospital = "Main Hospital",
            startTime = startTime,
            endTime = endTime,
            isRecurring = false
        });

        // act
        var query = $"hospital=Main&date={targetDate:yyyy-MM-dd}";
        var response = await _client.GetAsync($"/doctors/search?{query}");

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<List<DoctorResponse>>();

        // assert
        result.Should().NotBeNull();
        result!.Should().Contain(d => d.Id == doctor.Id);
    }

    [Fact]
    public async Task SearchDoctors_Should_Exclude_Doctors_Without_Availability_On_Date()
    {
        // create doctor without availability
        await _client.PostAsJsonAsync("/doctors", new CreateDoctorRequest
        {
            FullName = "Dr No Slots",
            Specialization = "General",
            Hospital = "Test"
        });

        var doctors = await _client.GetFromJsonAsync<List<DoctorResponse>>("/doctors");
        var doctor = doctors!.Single(d => d.FullName == "Dr No Slots");

        await _client.PutAsync($"/doctors/{doctor.Id}/approve", null);

        var targetDate = DateTime.UtcNow.Date.AddDays(2);

        var response = await _client.GetAsync($"/doctors/search?date={targetDate:yyyy-MM-dd}");

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<List<DoctorResponse>>();

        // assert doctor is NOT included
        result.Should().NotContain(d => d.Id == doctor.Id);
    }

    [Fact]
    public async Task SearchDoctors_Should_Return_Only_Approved_Doctors()
    {
        // create unapproved doctor
        await _client.PostAsJsonAsync("/doctors", new CreateDoctorRequest
        {
            FullName = "Dr Not Approved",
            Specialization = "General",
            Hospital = "Test"
        });

        var response = await _client.GetAsync("/doctors/search");

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<List<DoctorResponse>>();

        // assert unapproved doctor is excluded
        result.Should().NotContain(d => d.FullName == "Dr Not Approved");
    }

    [Fact]
    public async Task SearchDoctors_Should_Handle_Empty_Query_Returning_All_Approved()
    {
        await _client.PostAsJsonAsync("/doctors", new CreateDoctorRequest
        {
            FullName = "Dr All",
            Specialization = "General",
            Hospital = "Test"
        });

        var doctors = await _client.GetFromJsonAsync<List<DoctorResponse>>("/doctors");
        var doctor = doctors!.Single(d => d.FullName == "Dr All");

        await _client.PutAsync($"/doctors/{doctor.Id}/approve", null);

        var response = await _client.GetAsync("/doctors/search");

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<List<DoctorResponse>>();

        result.Should().NotBeNull();
        result!.Should().Contain(d => d.Id == doctor.Id);
    }

}

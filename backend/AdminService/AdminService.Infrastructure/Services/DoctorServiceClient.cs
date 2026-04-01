
using AdminService.Application.DTOs;
using AdminService.Application.Interfaces;
using System.Net.Http.Json;

public class DoctorServiceClient : IDoctorServiceClient
{
    private readonly HttpClient _client;

    public DoctorServiceClient(HttpClient client)
    {
        _client = client;
    }

    public async Task<List<DoctorResponse>> GetPendingDoctors()
    {
        return await _client.GetFromJsonAsync<List<DoctorResponse>>("/doctors/pending")
               ?? new();
    }

    public async Task ApproveDoctor(Guid id)
    {
        var response = await _client.PutAsync($"/doctors/{id}/approve", null);

        if (!response.IsSuccessStatusCode)
            throw new Exception("Failed to approve doctor");
    }
}
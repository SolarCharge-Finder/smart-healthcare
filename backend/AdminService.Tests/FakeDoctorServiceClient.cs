using AdminService.Application.DTOs;
using AdminService.Application.Interfaces;

namespace AdminService.Tests;

public class FakeDoctorServiceClient : IDoctorServiceClient
{
    public Task<List<DoctorResponse>> GetPendingDoctors()
    {
        return Task.FromResult(new List<DoctorResponse>
        {
            new DoctorResponse
            {
                Id = Guid.NewGuid(),
                FullName = "Dr Test",
                IsApproved = false
            }
        });
    }

    public Task ApproveDoctor(Guid id)
    {
        return Task.CompletedTask;
    }
}

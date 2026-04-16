using AdminService.Application.DTOs;
using AdminService.Application.Interfaces;

namespace AdminService.Tests;

public class FakeDoctorServiceClient : IDoctorServiceClient
{
    private readonly List<DoctorResponse> _doctors = new();

    public Task<List<DoctorResponse>> GetPendingDoctors()
    {
        return Task.FromResult(_doctors.Where(d => !d.IsApproved).ToList());
    }

    public Task ApproveDoctor(Guid id)
    {
        var doctor = _doctors.FirstOrDefault(d => d.Id == id);
        if (doctor != null)
        {
            doctor.IsApproved = true;
        }

        return Task.CompletedTask;
    }

    // helper for tests
    public void Seed(DoctorResponse doctor)
    {
        _doctors.Add(doctor);
    }
}

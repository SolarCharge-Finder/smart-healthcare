namespace Doctor.Application.Interfaces;

using Doctor.Application.DTOs;

public interface IDoctorService
{
    Task CreateDoctor(CreateDoctorRequest request);
    Task ApproveDoctor(Guid doctorId);
}
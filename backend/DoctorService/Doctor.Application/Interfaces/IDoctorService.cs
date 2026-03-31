namespace Doctor.Application.Interfaces;

public interface IDoctorService
{
    Task CreateDoctor(CreateDoctorRequest request);
    Task ApproveDoctor(Guid doctorId);
}
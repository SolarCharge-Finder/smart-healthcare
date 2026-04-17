namespace Doctor.Application.Interfaces;

using Doctor.Application.DTOs;

public interface IDoctorService
{
    Task<Guid> CreateDoctor(CreateDoctorRequest request, Guid userId);

    Task ApproveDoctor(Guid id);

    Task<List<DoctorResponse>> GetAll();

    Task<List<DoctorResponse>> GetApproved();

    Task<List<DoctorResponse>> SearchDoctors(SearchDoctorsRequest request);

    Task<FilterOptionsResponse> GetFilterOptionsAsync();

    Task<List<DoctorResponse>> GetPending();

    Task<DoctorResponse?> GetById(Guid id);

    Task<DoctorResponse?> GetByUserId(Guid userId);

    Task DeleteDoctor(Guid id);

    Task<decimal> GetConsultationFee(Guid doctorId);

    Task UpdateConsultationFee(Guid doctorId, decimal fee, Guid userId);

}

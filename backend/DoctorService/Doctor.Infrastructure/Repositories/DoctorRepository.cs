namespace Doctor.Infrastructure.Repositories;

using Doctor.Application.Interfaces;
using Doctor.Domain.Entities;
using Doctor.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

public class DoctorRepository : IDoctorRepository
{
    private readonly DoctorDbContext _context;

    private readonly bool _isPostgres;

    public DoctorRepository(DoctorDbContext context)
    {
        _context = context;
        _isPostgres = _context.Database.ProviderName!.Contains("Npgsql");
    }

    public async Task AddAsync(Doctor doctor)
    {
        await _context.Doctors.AddAsync(doctor);
    }

    public async Task<Doctor?> GetByIdAsync(Guid id)
    {
        return await _context.Doctors.FindAsync(id);
    }

    public async Task<Doctor?> GetByUserIdAsync(Guid userId)
    {
        return await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<List<Doctor>> GetApprovedAsync()
    {
        return await _context.Doctors
            .Where(d => d.IsApproved)
            .ToListAsync();
    }

    public async Task<List<Doctor>> SearchAsync(
        string? name,
        string? specialization,
        string? hospital,
        DateTime? date)
    {
        // pre normalize inputs
        name = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        specialization = string.IsNullOrWhiteSpace(specialization) ? null : specialization.Trim();
        hospital = string.IsNullOrWhiteSpace(hospital) ? null : hospital.Trim();

        var query = _context.Doctors
            .Where(d => d.IsApproved);

        if (!string.IsNullOrEmpty(name))
        {
            query = _isPostgres
                ? query.Where(d =>
                    EF.Functions.ILike(d.FullName, $"%{name}%"))
                : query.Where(d =>
                    d.FullName.ToLower().Contains(name.ToLower()));
        }

        if (!string.IsNullOrEmpty(specialization))
        {
            query = _isPostgres
                ? query.Where(d =>
                    EF.Functions.ILike(d.Specialization, $"%{specialization}%"))
                : query.Where(d =>
                    d.Specialization.ToLower().Contains(specialization.ToLower()));
        }

        // if (!string.IsNullOrEmpty(hospital))
        // {
        //     query = _isPostgres
        //         ? query.Where(d =>
        //             _context.DoctorAvailabilities.Any(a =>
        //                 a.DoctorId == d.Id &&
        //                 a.IsActive &&
        //                 EF.Functions.ILike(a.Hospital, $"%{hospital}%")))
        //         : query.Where(d =>
        //             _context.DoctorAvailabilities.Any(a =>
        //                 a.DoctorId == d.Id &&
        //                 a.IsActive &&
        //                 a.Hospital.Contains(hospital)));
        // }

        // availability filter combined since hospital is necessary to filter by availability
        //also technically it doesn't matter since we haven't yet implemented the multiple hospital thingy xdd
        //soon tm striked again 
        if (date.HasValue || hospital != null)
        {
            var startOfDay = date.HasValue
                ? DateTime.SpecifyKind(date.Value.Date, DateTimeKind.Utc)
                : (DateTime?)null;

            var endOfDay = startOfDay?.AddDays(1);

            query = query.Where(d =>
                _context.DoctorAvailabilities.Any(a =>
                    a.DoctorId == d.Id &&
                    a.IsActive &&
                    (!date.HasValue ||
                        (a.StartTime >= startOfDay && a.StartTime < endOfDay)) &&
                    (hospital == null ||
                        (_isPostgres
                            ? EF.Functions.ILike(a.Hospital, $"%{hospital}%")
                            : a.Hospital.ToLower().Contains(hospital.ToLower())
                        )
                    )
                ));
        }

        return await query.ToListAsync();
    }

    public async Task<List<Doctor>> GetAllAsync()
    {
        return await _context.Doctors.ToListAsync();
    }

    public async Task<List<Doctor>> GetPendingAsync()
    {
        return await _context.Doctors
            .Where(d => !d.IsApproved)
            .ToListAsync();
    }

    public Task RemoveAsync(Doctor doctor)
    {
        _context.Doctors.Remove(doctor);
        return Task.CompletedTask;
    }
}

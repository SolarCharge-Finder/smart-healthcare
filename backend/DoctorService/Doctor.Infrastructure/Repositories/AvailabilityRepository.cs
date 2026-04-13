namespace Doctor.Infrastructure.Repositories;

using Doctor.Application.Interfaces;
using Doctor.Domain.Entities;
using Doctor.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

public class AvailabilityRepository : IAvailabilityRepository
{
    private readonly DoctorDbContext _context;

    public AvailabilityRepository(DoctorDbContext context)
    {
        _context = context;
    }

    public async Task<DoctorAvailability> AddAsync(DoctorAvailability availability)
    {
        _context.DoctorAvailabilities.Add(availability);
        await _context.SaveChangesAsync();
        return availability;
    }

    public async Task<List<DoctorAvailability>> GetByDoctorIdAsync(Guid doctorId)
    {
        return await _context.DoctorAvailabilities
            .Where(a => a.DoctorId == doctorId)
            .ToListAsync();
    }

    public async Task<DoctorAvailability?> GetByIdAsync(Guid id)
    {
        return await _context.DoctorAvailabilities
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task UpdateAsync(DoctorAvailability availability)
    {
        _context.DoctorAvailabilities.Update(availability);
        await _context.SaveChangesAsync();
    }
}

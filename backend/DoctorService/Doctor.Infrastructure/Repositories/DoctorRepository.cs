namespace Doctor.Infrastructure.Repositories;

using Doctor.Application.Interfaces;
using Doctor.Domain.Entities;
using Doctor.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class DoctorRepository : IDoctorRepository
{
    private readonly DoctorDbContext _context;

    public DoctorRepository(DoctorDbContext context)
    {
        _context = context;
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
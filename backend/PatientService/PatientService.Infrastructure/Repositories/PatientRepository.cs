namespace PatientService.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;

using PatientService.Application.Interfaces;
using PatientService.Domain.Entities;
using PatientService.Infrastructure.Data;

public class PatientRepository : IPatientRepository
{
    private readonly PatientDbContext _context;

    public PatientRepository(PatientDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Patient patient)
    {
        await _context.Patients.AddAsync(patient);
    }

    public async Task<Patient?> GetByIdAsync(Guid id)
    {
        return await _context.Patients.FindAsync(id);
    }

    public async Task<Patient?> GetByUserIdAsync(Guid userId)
    {
        return await _context.Patients
            .FirstOrDefaultAsync(p => p.UserId == userId);
    }

    public async Task<List<Patient>> GetAllAsync()
    {
        return await _context.Patients.ToListAsync();
    }

    public Task RemoveAsync(Patient patient)
    {
        _context.Patients.Remove(patient);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}

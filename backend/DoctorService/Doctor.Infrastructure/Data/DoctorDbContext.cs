namespace Doctor.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Doctor.Domain.Entities;

public class DoctorDbContext : DbContext
{
    public DoctorDbContext(DbContextOptions<DoctorDbContext> options)
        : base(options) { }

    public DbSet<Doctor> Doctors => Set<Doctor>();
}
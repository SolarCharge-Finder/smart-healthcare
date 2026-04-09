namespace Doctor.Infrastructure.Data;

using Doctor.Domain.Entities;

using Microsoft.EntityFrameworkCore;

public class DoctorDbContext : DbContext
{
    public DoctorDbContext(DbContextOptions<DoctorDbContext> options)
        : base(options) { }

    public DbSet<Doctor> Doctors => Set<Doctor>();
}

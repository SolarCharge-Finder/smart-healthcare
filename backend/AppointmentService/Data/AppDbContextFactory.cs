using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AppointmentService.Data;

public class AppointmentDbContextFactory : IDesignTimeDbContextFactory<AppointmentDbContext>
{
    public AppointmentDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppointmentDbContext>();

        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=appointmentdb;Username=postgres;Password=admin"
        );

        return new AppointmentDbContext(optionsBuilder.Options);
    }
}
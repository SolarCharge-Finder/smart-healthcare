using Doctor.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DoctorService.Tests;

public class TestingFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var descriptor = services.FirstOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<DoctorDbContext>));

            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<DoctorDbContext>(options =>
                options.UseInMemoryDatabase("doctor-tests"));
        });
    }
}
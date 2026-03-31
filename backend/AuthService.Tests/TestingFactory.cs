using Auth.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AuthService.Tests;

public class TestingFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var descriptor = services.FirstOrDefault(
                d => d.ServiceType ==
                typeof(DbContextOptions<AuthDbContext>));

            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<AuthDbContext>(options =>
                options.UseInMemoryDatabase("auth-tests"));
        });
    }
}
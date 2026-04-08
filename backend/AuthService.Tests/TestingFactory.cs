using Auth.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Auth.Application.Interfaces;

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

            services.RemoveAll<IEmailService>();
            services.AddScoped<IEmailService, FakeEmailService>();
        });
    }
}
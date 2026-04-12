using Doctor.Application.Interfaces;
using Doctor.Infrastructure.Data;
using DoctorService.Tests.Fakes;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Testcontainers.PostgreSql;

namespace DoctorService.Tests;

public class PostgreSqlTestingFactory : TestingFactory
{
    private readonly PostgreSqlContainer _db;

    public PostgreSqlTestingFactory(PostgreSqlContainer db)
    {
        _db = db;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            var dict = new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-super-secret-key-123456789",
                ["Jwt:Issuer"] = "auth-service",
                ["Jwt:Audience"] = "smart-healthcare"
            };

            config.AddInMemoryCollection(dict);
        });

        builder.ConfigureServices(services =>
        {
            // replace DB
            var dbDescriptor = services.FirstOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<DoctorDbContext>));

            if (dbDescriptor != null)
                services.Remove(dbDescriptor);

            services.AddDbContext<DoctorDbContext>(options =>
                options.UseNpgsql(_db.GetConnectionString()));

            // replace auth client
            var authDescriptor = services.FirstOrDefault(
                d => d.ServiceType == typeof(IAuthServiceClient));

            if (authDescriptor != null)
                services.Remove(authDescriptor);

            services.AddScoped<IAuthServiceClient, FakeAuthServiceClient>();

            // 🚫 DO NOT TOUCH AUTHENTICATION HERE
        });
    }
}
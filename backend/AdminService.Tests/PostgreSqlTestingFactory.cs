using AdminService.Application.Interfaces;
using AdminService.Infrastructure.Data;

using AdminService.Tests.Fakes;

using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Shared.Contracts.Infrastructure.Auth;

using Testcontainers.PostgreSql;

namespace AdminService.Tests;

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

        // inject config (prevents AuthServiceClient crash)
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var dict = new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-super-secret-key-123456789",
                ["Jwt:Issuer"] = "auth-service",
                ["Jwt:Audience"] = "smart-healthcare",

                ["ServiceName"] = "admin-service",
                ["InternalApiKey"] = "test-api-key"
            };

            config.AddInMemoryCollection(dict);
        });

        builder.ConfigureServices(services =>
        {
            // replace DB with PostgreSQL container
            var dbDescriptor = services.FirstOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AdminDbContext>));

            if (dbDescriptor != null)
            {
                services.Remove(dbDescriptor);
            }

            services.AddDbContext<AdminDbContext>(options =>
                options.UseNpgsql(_db.GetConnectionString()));

            // replace doctor service client
            var doctorDescriptor = services.FirstOrDefault(
                d => d.ServiceType == typeof(IDoctorServiceClient));

            if (doctorDescriptor != null)
            {
                services.Remove(doctorDescriptor);
            }

            services.AddScoped<IDoctorServiceClient, FakeDoctorServiceClient>();

            // replace auth client
            var authDescriptor = services.FirstOrDefault(
                d => d.ServiceType == typeof(IAuthServiceClient));

            if (authDescriptor != null)
            {
                services.Remove(authDescriptor);
            }

            services.AddScoped<IAuthServiceClient, FakeAuthServiceClient>();
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);

        // default user for all tests
        client.DefaultRequestHeaders.Add("x-user-id", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("x-user-role", "Admin");
    }
}

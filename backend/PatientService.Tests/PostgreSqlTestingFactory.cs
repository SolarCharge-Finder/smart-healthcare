using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using PatientService.Infrastructure.Data;

using PatientService.Tests.Fakes;

using Shared.Contracts.Infrastructure.Auth;

using Testcontainers.PostgreSql;

namespace PatientService.Tests;

public class PostgreSqlTestingFactory : WebApplicationFactory<Program>
{
    private readonly PostgreSqlContainer _db;

    public PostgreSqlTestingFactory(PostgreSqlContainer db)
    {
        _db = db;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
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
                d => d.ServiceType == typeof(DbContextOptions<PatientDbContext>));

            if (dbDescriptor != null)
            {
                services.Remove(dbDescriptor);
            }

            services.AddDbContext<PatientDbContext>(options =>
                options.UseNpgsql(_db.GetConnectionString()));

            // replace auth client (IMPORTANT - matches Doctor setup)
            var authDescriptor = services.FirstOrDefault(
                d => d.ServiceType == typeof(IAuthServiceClient));

            if (authDescriptor != null)
            {
                services.Remove(authDescriptor);
            }

            services.AddScoped<IAuthServiceClient, FakeAuthServiceClient>();

            // test auth scheme
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
            });
        });
    }
}

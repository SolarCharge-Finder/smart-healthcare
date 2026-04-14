using System.Security.Claims;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using PatientService.Infrastructure.Data;

using PatientService.Tests.Fakes;

using Shared.Contracts.Infrastructure.Auth;

namespace PatientService.Tests;

public class TestingFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // inject JWT config (prevents null crash)
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
            // Replace DB with in-memory
            var dbDescriptor = services.FirstOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<PatientDbContext>));

            if (dbDescriptor != null)
            {
                services.Remove(dbDescriptor);
            }

            services.AddDbContext<PatientDbContext>(options =>
                options.UseInMemoryDatabase("patient-tests"));

            // replace auth client
            var authDescriptor = services.FirstOrDefault(
                d => d.ServiceType == typeof(IAuthServiceClient));

            if (authDescriptor != null)
            {
                services.Remove(authDescriptor);
            }

            services.AddScoped<IAuthServiceClient, FakeAuthServiceClient>();

            // Override authentication
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

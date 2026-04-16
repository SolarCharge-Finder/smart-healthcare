using System.Security.Claims;
using System.Text.Encodings.Web;

using AdminService.Application.Interfaces;
using AdminService.Infrastructure.Data;

using AdminService.Tests.Fakes;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Shared.Contracts.Infrastructure.Auth;

namespace AdminService.Tests;

public class TestingFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "admin-tests-" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Inject JWT + required config (prevents crashes)
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var dict = new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-super-secret-key-123456789",
                ["Jwt:Issuer"] = "auth-service",
                ["Jwt:Audience"] = "smart-healthcare",

                // REQUIRED for AuthServiceClient
                ["ServiceName"] = "admin-service",
                ["InternalApiKey"] = "test-api-key"
            };

            config.AddInMemoryCollection(dict);
        });

        builder.ConfigureServices(services =>
        {
            // Replace DB with in-memory
            var dbDescriptor = services.FirstOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AdminDbContext>));

            if (dbDescriptor != null)
            {
                services.Remove(dbDescriptor);
            }

            services.AddDbContext<AdminDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            // Replace Doctor client with fake
            var doctorDescriptor = services.FirstOrDefault(
                d => d.ServiceType == typeof(IDoctorServiceClient));

            if (doctorDescriptor != null)
            {
                services.Remove(doctorDescriptor);
            }

            services.AddSingleton<FakeDoctorServiceClient>();
            services.AddSingleton<IDoctorServiceClient>(sp =>
                sp.GetRequiredService<FakeDoctorServiceClient>());

            // Replace Auth client with fake (IMPORTANT)
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

    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);

        // Default user for all tests
        client.DefaultRequestHeaders.Add("x-user-id", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("x-user-role", "Admin");
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AdminDbContext>();

        context.Admins.RemoveRange(context.Admins);
        await context.SaveChangesAsync();
    }
}

using Auth.Application.Interfaces;
using Auth.Infrastructure.Data;

using AuthService.Tests.Fakes;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AuthService.Tests;

public class TestingFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "auth-tests-" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            var dict = new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-super-secret-key-123456789",
                ["Jwt:Issuer"] = "auth-service",
                ["Jwt:Audience"] = "smart-healthcare",

                ["ServiceName"] = "auth-service",

                // FIXED (this was breaking internal auth tests)
                ["InternalAuth:ApiKey"] = "test-api-key"
            };

            config.AddInMemoryCollection(dict);
        });

        builder.ConfigureServices(services =>
        {
            // Replace DB with InMemory
            var dbDescriptor = services.FirstOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AuthDbContext>));

            if (dbDescriptor != null)
            {
                services.Remove(dbDescriptor);
            }

            services.AddDbContext<AuthDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            // Inject fakes (REQUIRED)
            services.AddSingleton<IEmailService, FakeEmailService>();
            services.AddSingleton<ITokenService, FakeTokenService>();

            //Override authentication
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

        // default test user (can override in tests)
        client.DefaultRequestHeaders.Add("x-user-id", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("x-user-role", "Admin");
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        context.Users.RemoveRange(context.Users);
        await context.SaveChangesAsync();
    }
}
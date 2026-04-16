using Auth.Application.Interfaces;
using Auth.Infrastructure.Data;

using AuthService.Tests.Fakes;

using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Testcontainers.PostgreSql;

namespace AuthService.Tests;

public class PostgreSqlTestingFactory : TestingFactory
{
    private readonly PostgreSqlContainer _db;

    public PostgreSqlTestingFactory(PostgreSqlContainer db)
    {
        _db = db;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // keep base config (auth handler, etc.)
        base.ConfigureWebHost(builder);

        builder.UseEnvironment("Testing");

        // override config (must match main factory)
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var dict = new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-super-secret-key-123456789",
                ["Jwt:Issuer"] = "auth-service",
                ["Jwt:Audience"] = "smart-healthcare",

                ["ServiceName"] = "auth-service",

                ["InternalAuth:ApiKey"] = "test-api-key"
            };

            config.AddInMemoryCollection(dict);
        });

        builder.ConfigureServices(services =>
        {
            // Replace DB with PostgreSQL container
            var dbDescriptor = services.FirstOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AuthDbContext>));

            if (dbDescriptor != null)
            {
                services.Remove(dbDescriptor);
            }

            services.AddDbContext<AuthDbContext>(options =>
                options.UseNpgsql(_db.GetConnectionString()));

            // Inject required fakes (same as main factory)
            var emailDescriptor = services.FirstOrDefault(
                d => d.ServiceType == typeof(IEmailService));

            if (emailDescriptor != null)
            {
                services.Remove(emailDescriptor);
            }

            services.AddSingleton<IEmailService, FakeEmailService>();

            var tokenDescriptor = services.FirstOrDefault(
                d => d.ServiceType == typeof(ITokenService));

            if (tokenDescriptor != null)
            {
                services.Remove(tokenDescriptor);
            }

            services.AddSingleton<ITokenService, FakeTokenService>();
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);

        // default test identity
        client.DefaultRequestHeaders.Add("x-user-id", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("x-user-role", "Admin");
    }

    public new async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        context.Users.RemoveRange(context.Users);
        context.PendingUsers.RemoveRange(context.PendingUsers);

        await context.SaveChangesAsync();
    }
}
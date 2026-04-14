using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;

using AdminService.Application.Interfaces;
using AdminService.Infrastructure.Data;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AdminService.Tests;

public class TestingFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "TestDb_" + Guid.NewGuid();

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
            var descriptor = services.FirstOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AdminDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AdminDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            var doctorDescriptor = services.FirstOrDefault(
                d => d.ServiceType == typeof(IDoctorServiceClient));

            if (doctorDescriptor != null)
            {
                services.Remove(doctorDescriptor);
            }

            services.AddScoped<IDoctorServiceClient, FakeDoctorServiceClient>();

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

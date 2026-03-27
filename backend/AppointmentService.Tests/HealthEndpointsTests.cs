using AppointmentService.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Xunit;

namespace AppointmentService.Tests;

public class HealthEndpointsTests
    : IClassFixture<HealthEndpointsTests.TestingFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointsTests(TestingFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task LiveHealth_Returns_Alive()
    {
        var response = await _client.GetAsync("/health/live");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("alive", body);
    }

    public sealed class TestingFactory
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                var dbDescriptor = services
                    .FirstOrDefault(d =>
                        d.ServiceType ==
                        typeof(DbContextOptions<AppointmentDbContext>));

                if (dbDescriptor is not null)
                    services.Remove(dbDescriptor);

                services.AddDbContext<AppointmentDbContext>(options =>
                    options.UseInMemoryDatabase("appointment-tests"));

                var redisDescriptor = services
                    .FirstOrDefault(d =>
                        d.ServiceType ==
                        typeof(IConnectionMultiplexer));

                if (redisDescriptor is not null)
                    services.Remove(redisDescriptor);

                services.AddSingleton<IConnectionMultiplexer>(_ =>
                    ConnectionMultiplexer.Connect(
                        "localhost:6379,abortConnect=false,connectTimeout=100"));
            });
        }
    }
}

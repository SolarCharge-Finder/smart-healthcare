using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http;

using AdminService.Application.Interfaces;

namespace AdminService.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddHttpContextAccessor();

        services.AddTransient<AuthHeaderHandler>();

        services.AddHttpClient<IDoctorServiceClient, DoctorServiceClient>(client =>
        {
            client.BaseAddress = new Uri("http://doctor-service");
        })
        .AddHttpMessageHandler<AuthHeaderHandler>();

        return services;
    }
}
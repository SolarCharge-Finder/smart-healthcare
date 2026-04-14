using Prometheus;

namespace Auth.API.Extensions;

public static class ApplicationBuilderExtensions
{
    public static void UseApiMiddleware(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpMetrics();

        // cors 
        app.UseCors("AllowFrontend");

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapMetrics("/metrics/prometheus");

        app.MapControllers();
    }
}

namespace PatientService.API.Extensions;

using Prometheus;

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

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapMetrics("/metrics/prometheus");

        app.MapControllers();
    }
}

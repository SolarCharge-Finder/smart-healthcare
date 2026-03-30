namespace AIService.Middleware;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        const string headerName = "X-Correlation-ID";

        var correlationId = context.Request.Headers[headerName].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(correlationId))
            correlationId = Guid.NewGuid().ToString();

        context.Response.Headers[headerName] = correlationId;
        context.Items["CorrelationId"] = correlationId;

        using (Serilog.Context.LogContext.PushProperty("correlationId", correlationId))
        {
            await _next(context);
        }
    }
}

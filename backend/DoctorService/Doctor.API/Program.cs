using Doctor.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

// services
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddApiServices();

var app = builder.Build();

// middleware
app.UseApiMiddleware();

app.Run();

public partial class Program { }
using Doctor.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

// register services
builder.Services.AddApplicationServices(builder.Configuration);

// controllers + swagger
builder.Services.AddApiServices();

// authorization
builder.Services.AddAuthorization();

var app = builder.Build();

// middleware
app.UseApiMiddleware();

app.Run();

public partial class Program { }
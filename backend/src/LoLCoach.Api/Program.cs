using System.Reflection;
using FluentValidation;
using LoLCoach.Api.Application;
using LoLCoach.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "LoLCoach API",
        Version = "v1",
        Description = "API for LoLCoach player integrations."
    });
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory,
        $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
});

builder.Services.AddDbContext<PlayerDbContext>((services, options) =>
{
    var connection = services.GetRequiredService<IConfiguration>().GetConnectionString("LoLCoach")
        ?? throw new InvalidOperationException("Configure ConnectionStrings__LoLCoach before using persistence.");
    options.UseNpgsql(connection);
});
builder.Services.AddScoped<IPlayerRepository, PlayerRepository>();
builder.Services.AddScoped<IValidator<SearchPlayerCommand>, SearchPlayerValidator>();
builder.Services.AddScoped<SearchPlayerHandler>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHttpClient<IRiotAccountClient, RiotAccountClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});

var app = builder.Build();
app.UseExceptionHandler();

var swaggerEnabled = app.Environment.IsDevelopment() ||
    app.Configuration.GetValue<bool>("Swagger:Enabled");
if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "LoLCoach API v1"));
}

app.MapControllers();
app.Run();

public partial class Program { }

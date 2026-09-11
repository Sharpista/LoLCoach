using FluentValidation;
using LoLCoach.Api.Application;
using LoLCoach.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddControllers();

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
app.MapControllers();
app.Run();

public partial class Program { }

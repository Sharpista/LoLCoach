using System.Reflection;
using FluentValidation;
using LoLCoach.Api.Application;
using LoLCoach.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

const string CorsPolicyName = "AllowedOrigins";

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

var corsAllowedOrigins = GetAllowedOrigins(builder);
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        if (corsAllowedOrigins.Length == 0)
        {
            // Non-development with no configured origins: allow none. Never AllowAnyOrigin.
            policy.SetIsOriginAllowed(_ => false);
        }
        else
        {
            policy.WithOrigins(corsAllowedOrigins);
        }

        policy.WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
              .WithHeaders("Content-Type", "Authorization", "Accept");
    });
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

app.UseRouting();
app.UseCors(CorsPolicyName);
app.MapControllers();
app.Run();

static string[] GetAllowedOrigins(WebApplicationBuilder builder)
{
    var configured = builder.Configuration["CORS:AllowedOrigins"];
    if (!string.IsNullOrWhiteSpace(configured))
    {
        return configured.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    return builder.Environment.IsDevelopment()
        ? ["http://localhost:4200", "http://127.0.0.1:4200"]
        : [];
}

public partial class Program { }
